# DiskConverter
Studio mp4 converter code C# by LuukvandenHoogen


> Ik moest nog even schaven aan de archief-converter tool, of eigenlijk herschreven afgelopen dagen, maar hier is dan de code.
> Hij vangt nu beter de belangrijkste exceptions op. Bovendien maakte mijn eerste single-thread versie maar gebruik van 9% van de CPU,( jammer, voor onze gespierde multicore computers)
> dus even in verdiept en nu start de converter altijd 5 processen, die elk 6 threads draaien. Waardoor de converteer-tijd met 2/3 korter is geworden. (van 18 naar 6 uur per schijf van 6TB.
> Ik heb hem openbaar staan, want er geen enkele internet of login functie mee gemoeid geweest, gewoon een windows tooltje.
> Kun je hem clonen? weet niet hoe lang het account in de lucht blijft. 
> Taal:  C#  .NET 6
> Het is een FFMpeg wrapper, bij aanvang zoekt hij naar een Chocolatey folder, zodat installatie van ffmpeg via Chocolatey voldoende is om hem aan de gang te zetten.
