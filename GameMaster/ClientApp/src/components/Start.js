import React, { Component } from "react";

const error = e => {
  alert("Coś poszło nie tak, spróbuj ponownie.");
  console.log(`Error: ${e}`);
};

export class Start extends Component {
  constructor(props, context) {
    super(props, context);
    this.state = {};
    this.startGame = this.startGame.bind(this);
  }

  startGame(e) {
    const button = e.target;
    button.disabled = true;
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
