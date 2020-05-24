(function () {
	"use strict";

	const URL = "wss://localhost:5001/client";
	let board = null;

	class Player {
		constructor(id, team_number, is_leader) {
			this.id = id;
			this.team_number = team_number;
			this.is_leader = is_leader;
			this.instance = document.createElement("div");
			this.set_default();
			this.field = null;
		}

		remove_classes() {
			this.instance.classList.remove(...this.instance.classList);
		}

		set_default() {
			this.remove_classes();
			this.instance.classList.add(`player-${this.team_number}`);
		}

		set_piece() {
			this.remove_classes();
			this.instance.classList.add(`player-piece-${this.team_number}`);
		}

		move(field) {
			if (this.field !== null)
				this.field.remove_player();
			field.add_player(this);
		}
	}

	class Field {
		constructor(class_name) {
			this.default_class_name = class_name;
			this.instance = document.createElement("div");
			this.set_default();
			this.row = null;
			this.player = null;
		}

		remove_classes() {
			this.instance.classList.remove(...this.instance.classList);
		}

		set_default() {
			this.remove_classes();
			this.instance.classList.add(this.default_class_name);
		}

		add_row(row) {
			this.row = row;
			this.row.appendChild(this.instance)
		}

		add_player(player) {
			this.remove_player();
			this.player = player;
			this.player.field = this;
			this.instance.appendChild(this.player.instance);
		}

		remove_player() {
			if (this.player !== null) {
				this.instance.removeChild(this.player.instance);
				this.player.field = null;
				this.player = null;

			}
		}
	}

	class GoalField extends Field {
		constructor(team_number) {
			super(`goal-field-${team_number}`);
			this.team_number = team_number;
		}

		set_scored() {
			this.remove_classes();
			this.instance.classList.add(`goal-field-scored-${this.team_number}`);
		}
	}

	class NonGoalField extends Field {
		constructor(team_number) {
			super(`non-goal-field-${team_number}`);
			this.team_number = team_number;
		}
	}

	class TaskField extends Field {
		constructor() {
			super("task-field");
		}

		set_piece() {
			this.remove_classes();
			this.instance.classList.add("task-field-piece");
		}
	}

	class Board {
		constructor(width, height, first_goal_level, second_goal_level, goals) {
			this.width = width;
			this.height = height;
			this.first_goal_level = first_goal_level;
			this.second_goal_level = second_goal_level;
			this.score_1 = 0;
			this.score_2 = 0;
			this.score_1_field = document.getElementById("score-1");
			this.score_2_field = document.getElementById("score-2");
			this.info_field = document.getElementById("info-box");
			this.goals = goals;
			this.players = {};
			this.board_container = document.getElementById("board");
			this.board = this.generate();
			this.add_goal_fields();
			this.render();
		}

		set_scores() {
			this.score_1_field.innerHTML = this.score_1.toString();
			this.score_2_field.innerHTML = this.score_2.toString();
		}

		generateField(level, goal) {
			if (level >= this.second_goal_level) {
				if (goal)
					return new GoalField(2);
				return new NonGoalField(2);
			}
			if (level > this.first_goal_level) 
				return new TaskField();
			if (goal)
				return new GoalField(1);
			return new NonGoalField(1);
		}

		generate() {
			let board = new Array(this.height);
			for (let row_index = 0; row_index < this.height; ++row_index) {
				board[row_index] = new Array(this.width);
				for (let column_index = 0; column_index < this.width; ++column_index) {
					board[row_index][column_index] = this.generateField(row_index, false);
				}
			}
			return board;
		}

		add_goal_fields() {
			for (let i in this.goals) {
				let [x, y] = this.goals[i];
				this.board[y][x] = this.generateField(y, true);
			}
		}

		render() {
			this.board_container.innerHTML = "";
			for (let row_index = this.height -1; row_index >= 0; --row_index) {
				let row = document.createElement("div");
				row.classList.add("row");
				for (let column_index = 0; column_index < this.width; ++column_index) {
					this.board[row_index][column_index].add_row(row);
				}
				this.board_container.appendChild(row);
			}
			this.set_scores()
			this.info_field.innerHTML = "";
		}

		add_piece(x, y) {
			this.board[y][x].set_piece();
		}

		add_player(player) {
			this.players[player.id] = player;
		}

		destroy_piece(id) {
			this.players[id].set_default();
		}

		move(id, x, y) {
			this.players[id].move(this.board[y][x])
		}

		put(id) {
			let player = this.players[id];
			player.set_default();
			player.field.set_piece();
		}

		pick(id, contain_pieces) {
			let player = this.players[id];
			player.set_piece();
			if (contain_pieces)
				player.field.set_piece();
			else 
				player.field.set_default();
		}

		score_goal(id) {
			let player = this.players[id];
			player.set_default();
			player.field.set_scored();
			if (player.team_number === 1)
				++this.score_1;
			else
				++this.score_2;
			this.set_scores(); 
		}

		add_info(info) {
			let element = document.createElement("li");
			element.classList.add("list-group-item");
			element.innerHTML = info;
			this.info_field.insertBefore(element, this.info_field.firstChild);
		}
	}

	const on_open = (event) => {
		console.log("WebSocket is open");
	}

	const on_close = (event) => {
		console.log("WebSocket is closed");
	}

	const on_error = (event) => {
	  console.error("WebSocket error observed: ", event);
	}

	const on_message = (event) => {
		const msg = JSON.parse(event.data);
		const payload = msg.Payload;

		switch (msg.Type) {
			case "Init":
				board = new Board(payload.Width, payload.Height, payload.FirstGoalLevel, payload.SecondGoalLevel, 
					payload.Goals);
				break;

			case "Piece":
				board.add_piece(payload.X, payload.Y)
				break;

			case "Player":
				let player = new Player(payload.Id, payload.Team, payload.IsLeader);
				board.add_player(player);
				board.move(player.id, payload.X, payload.Y);
				break;

			case "Move":
				board.move(payload.Id, payload.X, payload.Y);
				break;

			case "Destroy":
				board.destroy_piece(payload.Id);
				break;

			case "Put":
				board.put(payload.Id);
				break;

			case "Goal":
				board.score_goal(payload.Id);
				break;

			case "Pick":
				board.pick(payload.Id, payload.ContainPieces);
				break;

			default:
				break;
		}

		if (msg.Info !== null) {
			board.add_info(msg.Info);
		}
	}

	const on_load = () => {
		let web_socket = new WebSocket(URL);
		web_socket.onopen = on_open;
		web_socket.onclose = on_close;
		web_socket.onerror = on_error;
		web_socket.onmessage = on_message;
	}

	const test = () => {
		const width = 5;
		const height = 10;
		const first_goal_level = 2;
		const second_goal_level = 7;
		const goals = [[2, 1], [2, 8]];

		board = new Board(width, height, first_goal_level, second_goal_level, goals);
		const player1 = new Player(1, 1, false);
		const player2 = new Player(2, 2, false);
		board.add_player(player1);
		board.add_player(player2);

		board.move(player1.id, 1, 4);
		board.move(player2.id, 4, 6);
	}

	window.onload = on_load;
})();
