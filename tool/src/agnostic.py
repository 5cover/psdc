from collections.abc import Mapping
from itertools import chain
from .util import NodeKind, println, csl, cslq


def intro():
    return 0


def enter_node(
        common_props: Mapping[str, str],
        lvl: int, parent: tuple[str, NodeKind],
        node: tuple[str, NodeKind],
        implements: Mapping[str, NodeKind],
        props: Mapping[str, str]):
    if reserved_props := props & common_props.keys():
        raise ValueError(f"reserved propety names in '{node[0]}': {cslq(reserved_props)}")

    base_list = ': ' + csl(implements) if implements else ''
    println(lvl, parent[0], '>', node[1].value, node[0], base_list)
    for pname, ptype in chain(props.items(), common_props.items()):
        println(lvl + 1, f'{pname} : {ptype}')


def exit_node(_): pass


def conclusion(): pass
