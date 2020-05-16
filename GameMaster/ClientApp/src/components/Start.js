import React, { Component } from "react";
import { error } from "../helpers/errors";
import { API_URL, NOTIFY_SHOW_TIME, API_REQUEST_INTERVAL } from "../helpers/constants";
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
    this.checkIfGameFinished = this.checkIfGameFinished.bind(this);
  }

  initGame(e) {
    const button = e.target;
    button.disabled = true;

    fetch(`${API_URL}/InitGame`, { method: "POST" }).then((res) => {
      if (res.ok) {
        notify.show("Gra zainicjalizowana", "success", NOTIFY_SHOW_TIME);
        this.setState({ gameInitialized: true, timer: setInterval(this.checkIfGameStarted, API_REQUEST_INTERVAL) });
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
        if (started === true) {
          notify.show("Gra wystartowała", "success", NOTIFY_SHOW_TIME);
          clearInterval(timer);
          this.setState({ gameStarted: true, timer: setInterval(this.checkIfGameFinished, API_REQUEST_INTERVAL) });
        }
      });
  }

  checkIfGameFinished() {
    const timer = this.state.timer;
    fetch(`${API_URL}/WasGameFinished`, { method: "GET" })
      .then(
        (res) => {
          if (res.ok) {
            return res.json();
          } else {
            notify.show("Gra skończona", "success", NOTIFY_SHOW_TIME);
            clearInterval(timer);
            this.setState({ gameFinished: true });
          }
        },
        (e) => {
          notify.show("Gra skończona", "success", NOTIFY_SHOW_TIME);
          clearInterval(timer);
          this.setState({ gameFinished: true });
        }
      )
      .then((finished) => {
        if (finished === true) {
          notify.show("Gra skończona", "success", NOTIFY_SHOW_TIME);
          clearInterval(timer);
          this.setState({ gameFinished: true });
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
