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
```c#
/* Init of SDS file */
using (SdsFile sdsFile = SdsFile.FromFile(@"E:\Games\Steam\steamapps\common\Mafia II Definitive Edition\pc\sds\mapa\mapa_city.sds"))
{
    /* Extracts all resources from SDS into selected directory*/
    sdsFile.ExportToDirectory(@"C:\Users\Mettak\Desktop");
    
    /* Extracts single resource to the selected path */
    Texture texture = sdsFile.GetResourceByTypeAndName<Texture>("map.dds");
    texture.Extract(@"C:\Users\Mettak\Desktop\map.dds");
    
    /* Extracts all textures from current SDS (if contains any) */
    sdsFile.ExtractResourcesByType<Texture>(@"C:\Users\Mettak\Desktop\mapa_city\textures");
    
    /* Replaces data of the selected file */
    MipMap mipMap = sdsFile.GetResourceByTypeAndName<MipMap>("map.dds");
    mipMap.ReplaceData(@"C:\Users\Mettak\Desktop\new_map.dds");
    
    /* Saves modified SDS file to the selected path */
    sdsFile.ExportToFile(@"C:\Users\Mettak\Desktop\modified.sds");
}
```

## Notice
* sdslib and all contents within this repository and/or organisation, are not affiliated with 2K Czech, 2K Games or Take-Two Interactive Software Inc.
* Mafia is registered trademark of Take-Two Interactive Software Inc.
