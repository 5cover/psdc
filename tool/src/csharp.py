from collections import OrderedDict
from itertools import chain
from collections.abc import Iterable, Mapping
from .util import camelize, println, pascalize, NodeKind, csl, cslq, get_dont_touch_me, remove_prefix
from functools import cache

Keywords = {
    'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch', 'char', 'checked', 'class', 'const', 'continue',
    'decimal', 'default', 'delegate', 'do', 'double', 'else', 'enum', 'event', 'explicit', 'extern', 'false', 'finally', 'fixed',
    'float', 'for', 'foreach', 'goto', 'if', 'implicit', 'in', 'int', 'interface', 'internal', 'is', 'lock', 'long', 'namespace',
    'new', 'null', 'object', 'operator', 'out', 'override', 'params', 'private', 'protected', 'public', 'readonly', 'ref',
    'return', 'sbyte', 'sealed', 'short', 'sizeof', 'stackalloc', 'static', 'string', 'struct', 'switch', 'this', 'throw', 'true',
    'try', 'typeof', 'uint', 'ulong', 'unchecked', 'unsafe', 'ushort', 'using', 'virtual', 'void', 'volatile', 'while'
}

NodeKinds = {NodeKind.Class: 'sealed class', NodeKind.Union: 'interface'}


def intro():
    print('using System.Diagnostics;')
    print()
    print('namespace Scover.Psdc.Parsing;')
    print()
    print('public interface Node')
    print('{')
    println(1, 'Range Location { get; }')
    print()
    return 1


def conclusion():
    print('}')


def enter_node(
        common_props: Mapping[str, str],
        lvl: int,
        parent: tuple[str, NodeKind],
        node: tuple[str, NodeKind],
        implements: Mapping[str, NodeKind],
        props: Mapping[str, str]):
    if reserved_props := props & common_props.keys():
        raise ValueError(f"reserved propety names in '{node[0]}': {cslq(reserved_props)}")

    props = OrderedDict(chain(common_props.items(), props.items()))
    require_explicit_constructor = any((requires_validation(t) for t in props.values()))
    println(lvl, f'public {NodeKinds[node[1]]} {pascalize(node[0])}', end='')
    if node[1] is NodeKind.Class and props and not require_explicit_constructor:
        print(f'({csl(map(argument, props.items()))})', end='')
    print(base_type_list((parent[0],) + tuple(implements.keys()) if parent[1] is NodeKind.Union else implements))
    println(lvl, '{')
    lvl += 1
    if node[1] is NodeKind.Class and props:
        if require_explicit_constructor:
            println(lvl, f'public {pascalize(node[0])}({csl(map(argument, props.items()))})')
            println(lvl, '{')
            for p in props.items():
                put_assignment(lvl + 1, *p)
            println(lvl, '}')
            for p in props.items():
                put_prop(lvl, node[0], *p, 'public')
        else:
            for p in props.items():
                put_prop(lvl, node[0], *p, 'public', True)


def exit_node(lvl: int):
    println(lvl, '}')


def argument(prop: tuple[str, str]):
    return f'{real_type(prop[1])} {camel_ident(prop[0])}'


def base_type_list(bases: Iterable[str]):
    return ' : ' + csl(map(pascalize, bases)) if bases else ''


def put_prop(lvl: int, type_name: str, name: str, type: str, access: str = '', put_init: bool = False):
    access = access + ' ' if access else ''
    init = ' = ' + camel_ident(name) + ';' if put_init else ''
    println(lvl, f'{access}{real_type(remove_prefix(type_name + ".", type))} {pascalize(name)} {{ get; }}{init}')


def put_assignment(lvl: int, name: str, type: str):
    if vexpr := validation_expr(camel_ident(name), type):
        println(lvl, f'Debug.Assert({vexpr});')
    println(lvl, f'{pascalize(name)} = {camel_ident(name)};')


def requires_validation(type: str):
    return '+' in type


@cache
def real_type(type: str) -> str:
    match type[-1]:
        case '?': return f'Option<{real_type(type[:-1])}>'
        case '+': return f'IReadOnlyList<{real_type(type[:-1])}>'
        case '*': return f'IReadOnlyList<{real_type(type[:-1])}>'
        case _: return pascalize(type)


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


def camel_ident(name: str):
    if s := get_dont_touch_me(name):
        return s
    name = camelize(name)
    return '@' + name if name in Keywords else name
