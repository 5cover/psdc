# Configuration

Name|Type|Default value|description
-|-|-|-
`ecrire-nl`|booléen|vrai|Whether to append a newline to format strings passed to `écrire` and `écrireÉcran`.
`<MSG_CODE>`|booléen|vrai|Whether to enable or disable a specific message code. Errors cannot be disabled.
`doc-head`|booléen|vrai|Whether to generate a Doxygen file header
`doc-body`|booléen|faux|Whether to generate Dodxgen documentation skeletons for functions, procedures, types and constants.
`file-brief`|`program` or chaîne|`program`|Doxygen file brief.<br>`program`: program name
`file-author`|`user` or chaîne|`user`|Doxygen file author.<br>`user`: current username
`file-date`|`today` or `modif` or chaîne|`today`|Doxygen file date.<br>`today`: system date<br>`modif`: date of file's last modification

## CLI representation

### Boolean options

`vrai` and `faux` are represented by `yes` and `no` respectively.

### Unions

Mutually exclusive groups:

- `file-date-today`, `file-date-modif`, `file-date <string>`.
- `file-brief-progname`, `file-brief <string>`
- `file-author-user`, `file-author <string>`

## JSON representation

### Boolean options

`vrai` and `faux` are represented by `true` and `false` respectively.

### Unions

Only keep the active member. "void" members have the`null` value:

```json
{
    "file-date": {
        "today": null,
    },
    "file-date": "today",
    "file-date": {
        "": "1/04/2400"
    }
}
```

## Preprocessor representation

todo

## C-specific

todo

Add blocks to switch cases: always, when multiple statements, only when variable
