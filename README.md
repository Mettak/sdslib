# sdslib
<b>sdslib</b> is small DLL library written in C# with whose help you can manage SDS files. This library gives you ability to create .NET applications for modding <a href="https://en.wikipedia.org/wiki/Mafia_II" target=_blank><b>Mafia II</b></a>. Development of this library requires reverse engineering.

## Features
* Extracting files
* Replacing files data
* Adding new files
* Creating custom SDS file
* Exporting modified SDS file

## SDS file
SDS data format is used by video-games (Mafia II and Mafia III); contains compressed data, such as textures, sounds, scripts, 3D models, that is extracted and loaded at runtime in <b>The Illusion Engine</b>. Following notes describes <b>version 19</b> of this file format (both versions of game are supported), I don't know how it works in Mafia III which uses version 20 but I think it will be similar.

* Data are compressed with <a href="https://www.zlib.net/" target=_blank><i>zlib</i></a> into 16 KB blocks.
* Uses <a href="https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function" target=_blank><i>FNV hash</i></a> function for checksums
* Some content like tables or XML files are encrypted with <a href="https://en.wikipedia.org/wiki/Tiny_Encryption_Algorithm" target=_blank><i>TEA</i></a> in Classic version (decrypting is not implemented in this library)

## Usage
Usage of this library is presented <a href="https://github.com/Mettak/sdslib/tree/master/sdslib.ConsoleAppSample" target=_blank>here</a> in console application sample project.

## Notice
* sdslib and all contents within this repository and/or organisation, are not affiliated with 2K Czech, 2K Games, Hangar 13 Games or Take-Two Interactive Software Inc.
* Mafia is registered trademark of Take-Two Interactive Software Inc.
