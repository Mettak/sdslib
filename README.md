# sdslib
<b>sdslib</b> is small DLL library written in C# with whose help you can manage SDS files. This library gives you ability to create .NET applications for modding <b>Mafia II</b>.

## Features
* Extracting files
* Replacing files

## SDS file
SDS data format is used by video-games (Mafia II and Mafia III); contains compressed data, such as textures, sounds, scripts, 3D models, that is extracted and loaded at runtime in <b>The Illusion Engine</b>.

* Data are compressed with zlib into 16 KB blocks.
* Uses FNV hash for header checksum
* Some files may be encrypted with TEA (which is not implemented in this library)

## Notice
* sdslib and all contents within this repository and/or organisation, are not affiliated with 2K Czech, 2K Games or Take-Two Interactive Software Inc.
* Mafia is registered trademark of Take-Two Interactive Software Inc.
