from collections.abc import Callable
from mkast import generate_ast, load_input, read_config
from mkast.cfg import FileConfig
from pathlib import Path
import yaml

kinds = ['Semantic', 'Syntax']

DIR = Path(__file__).parent

filename_config: Callable[[str], Path] = lambda k: DIR / f'config-{k.lower()}.yml'
filename_nodes = DIR / 'nodes.yml'
filename_code: Callable[[str], Path] = lambda k: DIR.parent / f'Psdc/Ast/{k}.generated.cs'

def load_config(filename: str | Path):
    with open(filename) as f:
        return FileConfig.model_validate(yaml.safe_load(f))

if __name__ == '__main__':
    for k in kinds:
        cfg = read_config(filename_config(k), load_config)
        with filename_nodes.open() as f:
            cfg_input, ast = load_input(f)
        code = generate_ast(cfg + cfg_input if cfg_input else cfg, ast)
        code_path = filename_code(k)
        code_path.parent.mkdir(parents=True, exist_ok=True)
        with code_path.open('w') as f:
            f.write(code)
