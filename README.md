# WiseUnpacker

[![Build and Test](https://github.com/mnadareski/WiseUnpacker/actions/workflows/build_and_test.yml/badge.svg)](https://github.com/mnadareski/WiseUnpacker/actions/workflows/build_and_test.yml)

This program is a wrapper around a C# port of the Wise installer extractor [REWise](https://codeberg.org/CYBERDEV/REWise). The library code has been expanded from the source, with many findings being reported back to the original project.

This code used to compile to a library, but all functionality included is now in [SabreTools.Serialization](https://github.com/SabreTools/SabreTools.Serialization). Do not use old versions of the package as there are critical issues found and fixed since it was integrated.

Older verions of this library were based on [E_WISE](https://kannegieser.net/veit/quelle/index_e.htm) and [Heuristic Wise-Setup Unpacker](http://www.angelfire.com/ego/jmeister/hwun/). The majority of the code implementation stemed from HWUN. The exception to this was the non-heuristic detection of archives based on the signatures detected by E_WISE.

## Releases

For the most recent stable build, download the latest release here: [Releases Page](https://github.com/mnadareski/WiseUnpacker/releases)

For the latest WIP build here: [Rolling Release](https://github.com/mnadareski/WiseUnpacker/releases/tag/rolling)

## Contributions

Contributions to the project are welcome. Please follow the current coding styles.
