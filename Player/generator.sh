#!/bin/bash
set -e

function usage {
    echo "USAGE:";
	echo "$0" "num" "team_1_args" "team_2_args";
	echo "num [number-of-players](def-2)";
	echo "team_1_args: \"red|blue [color](def-blue) 1+[strategy-num](def-1) 0|1 [background-run](def-0)\""
	echo "team_2_args: \"red|blue [color](def-red)  1+[strategy-num](def-1) 0|1 [background-run](def-0)\""
	echo "team_2_args can be empty to run 1 team"
}

num="${1-2}";
echo "Num=$num";
if (( $num < 1 || $num > 1000 )); then
	echo $"Wrong argument for number of players";
	usage;
	exit 1;
fi

team_1_args="${2-red 1 0}" 
team_2_args="${3-blue 1 0}" 

function trim {
	echo $*
}

function validate_team {
    echo $1;
	if [[ $1 != "red" && $1 != "blue" ]]; then
		echo "Wrong team for team_${2}";
		usage;
		exit 1;
	fi
}

function validate_strategy {
    echo $1;
	if ((  $1 < 1 )); then
		echo "Wrong strategy number for team_${2}";
		usage;
		exit 1;
	fi
}

function validate_bg {
    echo $1;
	if ((  $1 < 0 || $1 > 1 )); then
		echo "Wrong background option number for team_${1}";
		usage;
		exit 1;
	fi

}

function set_team_args {
	if [[ $1 -eq 1 ]]; then
		team_1="$(trim ${team_1_args:0:4})"
		validate_team $team_1 1;
		
		strat_1="$(trim ${team_1_args:4:2})"
		validate_strategy $strat_1 1
		
		bg_1="$(trim ${team_1_args:6:2})"
		validate_bg $bg_1 1;
	elif [[ $1 -eq 2 ]]; then
		team_2="$(trim ${team_2_args:0:4})"
		validate_team $team_2 2;
		
		strat_2="$(trim ${team_2_args:4:2})"
		validate_strategy $strat_2 2
		
		bg_2="$(trim ${team_2_args:6:2})"
		validate_bg $bg_2 2;
	fi;
}

team_1_run=false;
team_2_run=false;
if [[ "$team_1_args" != "" ]]; then
	set_team_args 1;
	team_1_run=true;
	echo "Team 1 set.";
fi
if [[ "$team_2_args" != "" ]]; then
	set_team_args 2;
	team_2_run=true;
	echo "Team 2 set.";
fi

# Port:0 means dynamically bound port
team1_comm="dotnet Player.dll TeamId=$team_1 Strategy=$strat_1 --urls https://127.0.0.1:0"
team2_comm="dotnet Player.dll TeamId=$team_2 Strategy=$strat_2 --urls https://127.0.0.1:0"


function handle_run {
    if [[ $1 != 0 ]]; then
		for i in `seq 1 $num`; do
			$2 &
		done
	elif [[ "$OSTYPE" == "msys" ]]; then
		for i in `seq 1 $num`; do
			cmd //c start cmd //k "$2" & disown
			sleep 1
		done
	else
		for i in `seq 1 $num`; do
			gnome-terminal -e "$2" & disown
			sleep 1
		done
		#TODO: Other terminals
	fi
}

cd bin/Debug/netcoreapp2.1

if [[ $team_1_run == "true" ]]; then
	handle_run $bg_1 "$team1_comm"
fi

if [[ $team_2_run == "true" ]]; then
	handle_run $bg_2 "$team2_comm"
fi


