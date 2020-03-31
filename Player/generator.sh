#!/bin/sh
set -x
set -e

num=${1-2}
# Port:0 means dynamically bound port
redComm='dotnet run TeamId=red --urls https://127.0.0.1:0'
blueComm='dotnet run TeamId=blue --urls https://127.0.0.1:0'


if [ "$OSTYPE" == "msys" ]; then
	for i in `seq 1 $num`; do
		cmd //c start cmd //k "$redComm" & disown
        cmd //c start cmd //k "$blueComm"  & disown
	done
else
	for i in `seq 1 $num`; do
		gnome-terminal -e "$redComm" & disown
        gnome-terminal -e "$blueComm" & disown
	done
	#TODO: rest
fi
