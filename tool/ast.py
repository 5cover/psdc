#!/usr/bin/env python3
from collections import OrderedDict
from collections.abc import Callable, Iterable, Mapping
from functools import cache
from os import path
from src.util import NodeKind, csl, cslq, is_do_not_touch
from typing import TypeGuard
import yaml

from src.csharp import intro, enter_node, exit_node, conclusion

AstNode = OrderedDict[str, 'AstNode | str'] | None

Filename = path.dirname(path.realpath(__file__)) + '/nodes.yml'

known_types = {'ident'}


def main():
    with open(Filename) as file:
        loader = yaml.SafeLoader
        loader.add_constructor(
            yaml.resolver.Resolver.DEFAULT_MAPPING_TAG,
            lambda loader, node: OrderedDict(loader.construct_pairs(node)),
        )
        ast: OrderedDict[str, AstNode] = yaml.load(file, loader)

    common_props = ('location', 'range'),
    lvl = intro()
    for name, node in ast.items():
        walk(lvl, ('node', NodeKind.Union), ast, name, node,
             lambda *args: enter_node(OrderedDict(common_props), *args),
             exit_node)
    conclusion()

# todo: instead of visiting on the fly, build a datastructure and revisit. this means we'll be able to query the properties and subnodes of a node when generating it, which will allow for smarter code generation (semi-colon body)

# invariant: reachable_nodes contains the current node

def walk(lvl: int, parent: tuple[str, NodeKind], reachable_nodes: OrderedDict[str, AstNode], name: str, node: AstNode,
         enter_node: Callable[[int, tuple[str, NodeKind], tuple[str, NodeKind], Mapping[str, NodeKind], Mapping[str, str]], None],
         exit_node: Callable[[int], None]):
    implements = OrderedDict(((k, NodeKind.Union) for k in in_unions(reachable_nodes, name) if k != parent[0]))
    if node_is_union(node):
        if redefined_nodes := {k for k in node & reachable_nodes.keys() if node[k] is not None}:
            raise ValueError(f"redefined nodes in '{name}': {cslq(redefined_nodes)}")

        me = name, NodeKind.Union
        enter_node(lvl, parent, me, implements, {})
        for sub in ((k, v) for k, v in node.items() if k not in reachable_nodes.keys()):
            walk(lvl + 1, me, node | reachable_nodes, *sub, enter_node, exit_node)
        exit_node(lvl)
    else:
        if node is None:
            node = OrderedDict()

        subs = subnodes(node)
        if redefined_subs := subs & reachable_nodes.keys():
            raise ValueError(f"redefined subnodes in '{name}': {cslq(redefined_subs)}")
        props = OrderedDict((k, v) for k, v in node.items() if isinstance(v, str))
        if undef_type_props := tuple(f"'{k}' ('{v}')" for k, v in props.items() if not check_type(reachable_nodes, v)):
            raise ValueError(f"properties of undefined type in '{name}': {csl(undef_type_props)}")

        me = name, NodeKind.Class
        enter_node(lvl, parent, me, implements, props)
        for sub in subs.items():
            walk(lvl + 1, me, subs | reachable_nodes, *sub, enter_node, exit_node)
        exit_node(lvl)


def check_type(reachable_nodes: OrderedDict[str, AstNode], ptype: str) -> bool:
    realtype = ptype.rstrip('*+?')
    if is_do_not_touch(ptype) or realtype in known_types:
        return True
    s = realtype.split('.', 1)
    if len(s) == 1:
        return s[0] in reachable_nodes.keys()
    first, others = s
    return check_type(reachable_nodes, first) and check_type(
        reachable_nodes | subnodes(reachable_nodes.get(first, None)), others)


def subnodes(node: AstNode) -> OrderedDict[str, AstNode]:
    return OrderedDict() if node is None else OrderedDict((k, v) for k, v in node.items() if not isinstance(v, str))


def in_unions(reachable_nodes: Mapping[str, AstNode], name: str) -> Iterable[str]:
    """ Returns the names of each union this node is in"""
    for k, v in reachable_nodes.items():
        if node_is_union(v):
            if name in v:
                yield k
        else:
            yield from in_unions(subnodes(v), name)

def pr(o, **kwargs):
    print(o, **kwargs)
    return o

def node_is_union(node: AstNode) -> TypeGuard[OrderedDict[str, AstNode]]:
    return node is not None and not any(isinstance(v, str) for v in node.values())


@cache
def validation_expr(name: str, type: str):
    match type[-1]:
        case '?':
            inner = validation_expr(name + '.Value', type[:-1])
            return f'!{name}.HasValue || {inner}' if inner else ''
        case '+':
            inner = validation_expr(name, type[:-1])
            return f'{name}.Count > 0' + (f' && {name}.All({name} => {inner})' if inner else '')
        case '*':
            inner = validation_expr(name, type[:-1])
            return f'{name}.All({name} => {inner})' if inner else ''
        case _: return ''


if __name__ == '__main__':
    main()
