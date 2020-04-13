#!/bin/sh
set -x
set -e

num=${1-2}
background=${2-0}
# Port:0 means dynamically bound port
redComm='dotnet run TeamId=red --urls https://127.0.0.1:0'
blueComm='dotnet run TeamId=blue --urls https://127.0.0.1:0'

if [[ $background != 0 ]]; then
	for i in `seq 1 $num`; do
		$redComm &
		$blueComm &
	done	
elif [[ "$OSTYPE" == "msys" ]]; then
	for i in `seq 1 $num`; do
		cmd //c start cmd //k "$redComm" & disown
        sleep 1
		cmd //c start cmd //k "$blueComm"  & disown
		sleep 1
	done
else
	for i in `seq 1 $num`; do
		gnome-terminal -e "$redComm" & disown
		sleep 1
        gnome-terminal -e "$blueComm" & disown
		sleep 1
	done
	#TODO: rest
fi
