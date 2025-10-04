# Contributing to Psdc

## Grammar and Parser Generation

Psdc uses [ANTLR-ng](https://www.npmjs.com/package/antlr-ng) to generate the C# lexer and parser from the Pseudocode grammar.  
ANTLR-ng is a maintained fork of ANTLR 4 (last updated 2025) and is distributed as an npm package.

### Why npm?

ANTLR-ng is published on npm and written in TypeScript.  
We use a `package.json` in this repository to pin the tool version and make the grammar build reproducible across machines and CI.  

### When do I need Node.js?

- **Yes**: if you are editing the grammar (`*.g4` files). You’ll need Node.js ≥ 18 and npm to regenerate parser sources.  
- **No**: if you are working on the compiler, analyzer, codegen, or documentation only. Pre-generated parser sources are checked into the repo, so you don’t need Node.js to build Psdc.

### Workflow for Grammar Changes

1. Install dependencies:

   ```bash
   npm ci
   ```

2. Regenerate the parser:

   ```bash
   npm run generate
   ```

   This will:

   - Run `antlr-ng` on the grammar files
   - Emit updated C# parser/lexer classes into `src/Parsing/Generated/`

3. Commit both your grammar changes and the regenerated C# sources.

### Notes

- Do not edit the generated parser/lexer files by hand. They will be overwritten.
- If you are reviewing a PR and see large diffs in `Generated/`, focus on the grammar changes instead.
