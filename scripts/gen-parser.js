import { spawnSync } from 'child_process';
import { mkdirSync } from 'fs';
import { join, dirname } from 'path';

const root = dirname(import.meta.dirname);

for (const v of ['23', '24']) {
    const parts = ['Psdc', 'Parsing', `Pseudocode${v}`, 'Generated'];
    const outputDir = join(root, ...parts);
    mkdirSync(outputDir, { recursive: true });
    const grammarFile = side => join(root, 'ast', v, `Pseudocode${v}${side}.g4`);
    spawnSync('npx', ['--', 'antlr-ng',
        '-Dlanguage=CSharp',
        '-w','-v',
        '-o', outputDir,
        '-p', ['Scover', ...parts].join('.'),
        grammarFile('Lexer'), grammarFile('Parser')], {
            stdio: 'inherit'
        });
        console.log(['antlr-ng',
        '-Dlanguage=csharp',
        '-w','-v',
        '--message-format', 'gnu',
        '-o', outputDir,
        '-p', ['Scover', ...parts].join('.'),
        grammarFile('Lexer'), grammarFile('Parser')].join(' '))
}
