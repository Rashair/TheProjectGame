#!/bin/sh
num=${1-2}
sourceFile=${2-netcoreapp2.1}
set -x

cd "bin/Debug";

for i in `seq 1 $num`
do
	f0="blue-${i}"
	f2="red-${i}"
	
	rm -rf "$f0"
	cp -r "${sourceFile}/" "$f0"
	rm -rf "$f2"
	cp -r "${sourceFile}/" "$f2"
	
	defP=6001
	port0=$(expr 6000 + $i)
	sed -i "s/$defP/$port0/" "${f0}/appsettings.Development.json"
    sed -i "s/$defP/$port0/" "${f0}/appsettings.json"
	port2=$(expr 6000 + $i + $num)
	sed -i "s/blue/red/; s/$defP/$port2/" "${f2}/appsettings.Development.json"
    sed -i "s/blue/red/; s/$defP/$port2/" "${f2}/appsettings.json"
	
	if [ "$OSTYPE" == "msys" ]; then
		cd "$f0";
		cmd //c start cmd //k  "dotnet player.dll" & disown
		cd "../$f2";
		cmd //c start cmd //k  "dotnet player.dll" & disown
	else 
		cd "$f0";
		gnome-terminal -e "dotnet player.dll" & disown
		cd "../$f2";
		gnome-terminal -e "dotnet player.dll" & disown
		# TODO: rest
	fi
	cd ".."
	
done
