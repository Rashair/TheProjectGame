import React, { Component } from "react";
import { error } from "../helpers/errors";
import { API_URL } from "../helpers/constants";

export class Start extends Component {
  constructor(props, context) {
    super(props, context);
    this.state = {};
    this.startGame = this.startGame.bind(this);
  }

  startGame(e) {
    const button = e.target;
    button.disabled = true;

    fetch(`${API_URL}/startGame`, { method: "POST" }).then(res => {
      if (res.ok) {
        alert("Gra rozpoczeta");
      } else {
        error(res);
      }
    }, error);
  }
  componentDidMount() {}

  render() {
    return (
      <div className="text-center">
        <h1 className="mb-5">Start gry</h1>
        <input className="btn btn-success btn-lg p-3" type="submit" value="Start" onClick={this.startGame} />
      </div>
    );
  }
}
