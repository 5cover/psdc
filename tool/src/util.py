from collections.abc import Iterable
from enum import Enum
from functools import cache
import re


def is_do_not_touch(s: str) -> bool:
    return s.startswith('=')


def get_dont_touch_me(s: str) -> str:
    return s[1:] if is_do_not_touch(s) else ''


class NodeKind(Enum):
    Union = 'union'
    Class = 'class'


def println(lvl: int, *args, **kwargs):
    print(lvl * 4 * ' ', end='')
    print(*args, **kwargs)


def remove_prefix(prefix: str, s: str) -> str:
    return s[len(prefix):] if s.startswith(prefix) else s


@cache
def pascalize(snake_case: str) -> str:
    """
    Camelizes the string and replaces its first non-underscore, non_dot character by its uppercase equivalent.
    """
    if s := get_dont_touch_me(snake_case):
        return s
    c = camelize(snake_case)
    return re.sub(r'^([_.]*)([^_.])', lambda m: m.group(1) + m.group(2).upper(), c)


@cache
def camelize(snake_case: str) -> str:
    """
    Replace undescores which are not the first character of the string and are followed by a non-undescore character by the uppercase equivalent of that character.
    """
    if s := get_dont_touch_me(snake_case):
        return s
    return re.sub(r'(?<!^)(?:_|(?<=\.))([^_.])', lambda m: m.group()[-1].upper(), snake_case)


def cslq(iterable: Iterable[str]) -> str:
    """Comma Separated List (Quoted)"""
    return csl(f"'{i}'" for i in iterable)


def csl(iterable: Iterable[str]) -> str:
    """Comma Separated List"""
    return ', '.join(iterable)
