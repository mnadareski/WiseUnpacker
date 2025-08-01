# WiseUnpacker

[![Build and Test](https://github.com/mnadareski/WiseUnpacker/actions/workflows/build_and_test.yml/badge.svg)](https://github.com/mnadareski/WiseUnpacker/actions/workflows/build_and_test.yml)

C# port of the Wise installer extractors [REWise](https://codeberg.org/CYBERDEV/REWise), [E_WISE](https://kannegieser.net/veit/quelle/index_e.htm), and [Heuristic Wise-Setup Unpacker](http://www.angelfire.com/ego/jmeister/hwun/). This currently compiles as a library so it can be used in any C# application. For an example of usage, see [Binary Object Scanner](https://github.com/SabreTools/BinaryObjectScanner).

Of note, the majority of the code implementation stems from HWUN. The exception to this is the non-heuristic detection of archives based on the signatures detected by E_WISE. This should provide a stable combination of extraction for any unknown variants.

Find the link to the Nuget package [here](https://www.nuget.org/packages/WiseUnpacker).

## Releases

For the most recent stable build, download the latest release here: [Releases Page](https://github.com/mnadareski/WiseUnpacker/releases)

For the latest WIP build here: [Rolling Release](https://github.com/mnadareski/WiseUnpacker/releases/tag/rolling)

## Contributions

Contributions to the project are welcome. Please follow the current coding styles.
