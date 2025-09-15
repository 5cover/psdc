# dependency: pip install mkast

from collections.abc import Callable
from mkast import generate_ast, load_input, read_config
from mkast.cfg import FileConfig
from pathlib import Path
import yaml

versions = ['24']
kinds = ['semantic', 'syntax']

DIR = Path(__file__).parent

filename_config: Callable[[str], Path] = lambda k: DIR / f'config-{k}.yml'
filename_nodes: Callable[[str], Path] = lambda v: DIR / v / 'nodes.yml'
filename_code: Callable[[str, str], Path] = lambda v, k: DIR.parent / f'Psdc/Ast/Pseudocode{v}/{k[0].upper()}{k[1:]}.cs'

def load_config(filename: str | Path):
    with open(filename) as f:
        return FileConfig.model_validate(yaml.safe_load(f))

if __name__ == '__main__':
    for k in kinds:
        cfg = read_config(filename_config(k), load_config)
        for v in versions:
            with filename_nodes(v).open() as f:
                cfg_input, ast = load_input(f)
            code = generate_ast(cfg + cfg_input if cfg_input else cfg, ast)
            code_path = filename_code(v, k)
            code_path.parent.mkdir(parents=True, exist_ok=True)
            with code_path.open('w') as f:
                f.write(code)
