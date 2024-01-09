# PSDC

Pseudocode compiler.
Translates Pseudocode to C and compiles it.

Fully configurable.

Pseudocode flavor: IUT de Lannion

## CLI

Syntax: `psdc <options> <input files> <compiler options>`

> [!IMPORTANT]
> `psdc` options must be specified before the input files.

**Parameters**:

Short name | Long name | Description | Expected value | Default value
-|-|-|-|-
-c | --ccompiler | Path the C compiler binary to use. | A path | `gcc`
-f | --filesmode | Specified the behavior when there are multiple input files  | `concat` - Concatenate the files and pass the result as a single file to the C compiler.<br>`together` - Pass all files to the C compiler. | `together`

**Switches**:

Short name | Long name | Description
-|-|-
N/A | --ecrire-noNewline | Changes the behavior of the `ecrireEcran` subroutine so it doesn't print a newline | switch parameter

## Tools

[Visual Studio Code Pseudocode extension](https://marketplace.visualstudio.com/items?itemName=NoanPerrot.pseudocode) (by Noan Perrot)
