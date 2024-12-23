from .util import NodeKind, println, csl, cslq

def intro(): pass

def enter_node(common_props: dict[str, str], lvl: int, node: tuple[str, NodeKind], parents: dict[str, NodeKind], props: dict[str, str]):
    if reserved_props := props & common_props.keys():
        raise ValueError(f"reserved propety names in '{node[0]}': {cslq(reserved_props)}")

    println(lvl, node[1].value, node[0], ':', csl(parents))
    for pname, ptype in (props | common_props).items():
        println(lvl + 1, f'{pname} : {ptype}')

def exit_node(_): pass

def conclusion(): pass