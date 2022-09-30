# INFO
This project is currently suspended. I'm not planning working on it in early future. There is much more better tool called <a href="https://github.com/Greavesy1899/MafiaToolkit" target=_blank><b>Mafia: Toolkit</b></a>, so please, use it instead.

# sdslib
<b>sdslib</b> is a .NET library written in C# for manage SDS files. This library gives you ability to create .NET applications for modding games which are using <b>The Illusion Engine</b>. Development of this library requires reverse engineering.

## Features
### Version 19
* Extracting files
* Replacing files data
* Adding new files
* Creating custom SDS file
* Exporting modified SDS file

### Version 20
* Extracting files

## SDS file
SDS data format is used by video-games (Mafia II, Mafia III and Mafia: DE); contains compressed data, such as textures, sounds, scripts, 3D models, etc.. that is extracted and loaded at runtime in <b>The Illusion Engine</b>.

* Data are compressed with <a href="https://www.zlib.net/" target=_blank><i>zlib</i></a> into 16 KB blocks in <b>version 19</b>.
* <b>Version 20</b> compressing data with <a href="http://www.radgametools.com/oodlecompressors.htm" target=_blank><i>Oodle</i></a> into 64 KB blocks
* Uses <a href="https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function" target=_blank><i>FNV hash</i></a> function for checksums
* Some content like tables or XML files are encrypted with <a href="https://en.wikipedia.org/wiki/Tiny_Encryption_Algorithm" target=_blank><i>TEA</i></a> in Mafia II Classic (decrypting is not implemented in this library)

In the future I'm going to create wiki page with SDS file specification in more detail.

## Usage
Usage of this library is presented <a href="https://github.com/Mettak/sdslib/tree/master/sdslib.ConsoleAppSample" target=_blank>here</a> in console application sample project.

## Notice
* sdslib and all contents within this repository and/or organisation, are not affiliated with 2K Czech, 2K Games, Hangar 13 Games or Take-Two Interactive Software Inc.
* Mafia is registered trademark of Take-Two Interactive Software Inc.
