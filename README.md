# WiseUnpacker

C# port of the Wise installer extractors [E_WISE](https://kannegieser.net/veit/quelle/index_e.htm) and [Heuristic Wise-Setup Unpacker](http://www.angelfire.com/ego/jmeister/hwun/). This currently compiles as a library so it can be used in any C# application. For an example of usage, see [Binary Object Scanner](https://github.com/SabreTools/BinaryObjectScanner).

Of note, the majority of the code implementation stems from HWUN. The exception to this is the non-heuristic detection of archives based on the signatures detected by E_WISE. This should provide a stable combination of extraction for any unknown variants.

## Contributions

Contributions to the project are welcome. Please follow the current coding styles.

