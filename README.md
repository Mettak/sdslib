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

## Usage
```c#
/* Init of SDS file */
SdsFile sds = new SdsFile(@"...Mafia II\pc\sds\mapa\mapa_city.sds");

/* Extracts all files from SDS into selected directory */
sds.ExtractAllFiles(@"C:\Users\Mettak\Desktop\mapa_city");

/* Extracts single file to the selected path */
sds.GetFiles()[0].Extract(@"C:\Users\Mettak\Desktop\map.dds");

/* Extracts all textures from current SDS (if contains any) */
foreach (var File in sds.GetFiles())
  if (File is Texture)
    File.Extract(@"C:\Users\Mettak\tex\" + File.GetName());

/* Replace file */
sds.GetFiles()[0].Replace(@"C:\Users\Mettak\Desktop\modified_map.dds");

/* Saves modified SDS file */
sds.Save();

/* Saves modified SDS file to the selected path */
sds.Save(@"C:\Users\Mettak\Desktop\modified.sds");
```

## Notice
* sdslib and all contents within this repository and/or organisation, are not affiliated with 2K Czech, 2K Games or Take-Two Interactive Software Inc.
* Mafia is registered trademark of Take-Two Interactive Software Inc.
