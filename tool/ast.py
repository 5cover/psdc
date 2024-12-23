#!/usr/bin/env python3
from collections.abc import Callable, Mapping
from frozendict import frozendict  # frozendict preserves order
from functools import cache
from os import path
from src.agnostic import intro, enter_node, exit_node, conclusion
from src.util import NodeKind, cslq
from typing import TypeGuard
import yaml

AstNode = frozendict[str, 'AstNode | str'] | None

Filename = path.dirname(path.realpath(__file__)) + '/nodes.yml'


def main():
    with open(Filename) as file:
        loader = yaml.SafeLoader
        loader.add_constructor(
            yaml.resolver.Resolver.DEFAULT_MAPPING_TAG,
            lambda loader, node: frozendict(loader.construct_pairs(node)),
        )
        ast: frozendict[str, AstNode] = yaml.load(file, loader)

    intro()
    walk(1, ('node', NodeKind.Union), ast, ast,
         lambda *args: enter_node({'location': 'range'}, *args),
         exit_node)
    conclusion()

# todo: cleanup and test ancestor node reachability. do we need two arguments nodes and reachable_nodes
# todo: instead of visiting on the fly, build a datastructure and revisit. this means we'll be able to query the properties and subnodes of a node when generating it, which will allow for smarter code generation (refer to subtype with dot notation automatically, semi-colon body)
# todo: typecheck property types not marked as do not touch

def walk(lvl: int, parent: tuple[str, NodeKind], nodes: Mapping[str, AstNode], reachable_nodes: frozendict[str, AstNode],
         enter_node: Callable[[int, tuple[str, NodeKind], dict[str, NodeKind], dict[str, str]], None],
         exit_node: Callable[[int], None] = lambda _: None):
    for name, node in nodes.items():
        if node_is_union(node):
            if redefined_nodes := {k for k in node & reachable_nodes.keys() if node[k] is not None}:
                raise ValueError(f"redefined nodes in '{name}': {cslq(redefined_nodes)}")
            n = name, NodeKind.Union
            enter_node(lvl, n, {parent[0]: parent[1]}, {})
            walk(lvl + 1, n, {k: node[k] for k in node - nodes.keys()}, reachable_nodes | node, enter_node, exit_node)
            exit_node(lvl)
        else:
            if node is None:
                node = {}
            members = {k: v for k, v in node.items() if not isinstance(v, str)}
            props = {k: v for k, v in node.items() if isinstance(v, str)}
            if redefined_members := members & reachable_nodes.keys():
                raise ValueError(f"redefined members in '{name}': {cslq(redefined_members)}")
            n = name, NodeKind.Class
            in_unions = {k: NodeKind.Union for k, v in nodes.items() if node_is_union(v) and name in v}
            in_unions[parent[0]] = parent[1]
            enter_node(lvl, n, in_unions, props)
            walk(lvl + 1, n, members, reachable_nodes | members, enter_node, exit_node)
            exit_node(lvl)


def node_is_union(node: AstNode) -> TypeGuard[frozendict[str, AstNode]]:
    return node is not None and all(not isinstance(v, str) for v in node.values())


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
