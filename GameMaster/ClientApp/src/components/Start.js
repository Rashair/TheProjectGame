import React, { Component } from "react";
import { error } from "../helpers/errors";
import { API_URL, NOTIFY_SHOW_TIME } from "../helpers/constants";
import { notify } from "react-notify-toast";

export class Start extends Component {
  constructor(props, context) {
    super(props, context);
    this.state = {
      gameInitialized: false,
      gameStarted: false,
      gameFinished: false,
      timer: {},
    };
    this.initGame = this.initGame.bind(this);
    this.checkIfGameStarted = this.checkIfGameStarted.bind(this);
  }

  initGame(e) {
    const button = e.target;
    button.disabled = true;

    fetch(`${API_URL}/InitGame`, { method: "POST" }).then((res) => {
      if (res.ok) {
        notify.show("Gra zainicjalizowana", "success", NOTIFY_SHOW_TIME);
        this.setState({ gameInitialized: true, timer: setInterval(this.checkIfGameStarted, 10000) });
      } else {
        error(res);
      }
    }, error);
  }

  checkIfGameStarted() {
    const timer = this.state.timer;
    fetch(`${API_URL}/WasGameStarted`, { method: "GET" })
      .then(
        (res) => {
          if (res.ok) {
            return res.json();
          } else {
            clearInterval(timer);
            error(res);
          }
        },
        (e) => {
          clearInterval(timer);
          error(e);
        }
      )
      .then((started) => {
        console.log(`Started: ${started}`);
        if (started === true) {
          notify.show("Gra wystartowała", "success", NOTIFY_SHOW_TIME);
          this.setState({ gameStarted: true });
          clearInterval(timer);
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
