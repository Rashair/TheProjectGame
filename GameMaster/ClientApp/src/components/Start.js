import React, { Component } from "react";
import { error } from "../helpers/errors";
import { API_URL } from "../helpers/constants";

export class Start extends Component {
  constructor(props, context) {
    super(props, context);
    this.state = {
      gameInitialized: false,
      gameStarted: false,
      gameFinished: false,
    };
    this.initGame = this.initGame.bind(this);
  }

  initGame(e) {
    const button = e.target;
    button.disabled = true;

    fetch(`${API_URL}/InitGame`, { method: "POST" }).then(res => {
      if (res.ok) {
        alert("Gra zainicjalizowana");
        this.setState({ gameInitialized: true });
        this.timer = setInterval(this.checkIfGameStarted, 3000);
      } else {
        error(res);
      }
    }, error);
  }

  checkIfGameStarted() {
    fetch(`${API_URL}/WasGameStarted`, { method: "GET" })
      .then(
        res => {
          if (res.ok) {
            return res.json();
          } else {
            clearInterval(this.timer);
            error(res);
          }
        },
        e => {
          clearInterval(this.timer);
          error(e);
        }
      )
      .then(started => {
        console.log(`Started: ${started}`);
        if (started === true) {
          alert("Gra wystartowała");
          this.setState({ gameStarted: true });
          clearInterval(this.timer);
        }
      });
  }

  render() {
    const { gameInitialized, gameStarted, gameFinished } = this.state;
    return (
      <div className="d-flex align-items-center flex-column">
        <h1>Start gry</h1>
        <div className="mt-3">
          <button
            id="init"
            className="btn btn-success btn-lg p-3 col"
            disabled={gameInitialized ? "disabled" : ""}
            onClick={this.initGame}
          >
            Inicjalizacja gry
          </button>
        </div>
        <div className="border-info font-weight-bold mt-5">
          {gameInitialized && gameStarted && !gameFinished && <div>Trwa rozgrywka...</div>}
          {gameFinished && <div> Gra zakończona !!! </div>}
        </div>
      </div>
    );
  }
}
