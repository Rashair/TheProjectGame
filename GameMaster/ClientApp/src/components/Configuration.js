import React, { Component } from "react";
import { error } from "../helpers/errors";
import { API_URL } from "../helpers/constants";

const CustomFieldset = ({ id, label, type = "number", min = 0, value, onChange }) => {
  return (
    <fieldset className="form-group col-md-4 mb-4">
      <label className="ml-1" htmlFor={id}>
        {label ? label : id}
      </label>
      <input className="form-control" type={type} id={id} min={min} value={value} onChange={onChange} />
    </fieldset>
  );
};

export class Configuration extends Component {
  constructor(props, context) {
    super(props, context);
    this.state = {
      csIP: "",
      csPort: 0,
      movePenalty: 0,
      askPenalty: 0,
      discoverPenalty: 0,
      putPenalty: 0,
      checkPenalty: 0,
      responsePenalty: 0,
      width: 0,
      height: 0,
      goalAreaHeight: 0,
      numberOfGoals: 0,
      numberOfPlayersPerTeam: 0,
      shamPieceProbability: 0,
      numberOfPiecesOnBoard: 0,
    };

    this.sendData = this.sendData.bind(this);
  }

  componentDidMount() {
    fetch(`${API_URL}/configuration`)
      .then(res => {
        if (res.ok) {
          return res.json();
        } else {
          error(res);
        }
      }, error)
      .then(json => this.setState(json), error);
  }

  sendData(event) {
    event.preventDefault();

    if (this.state.goalAreaHeight * this.state.width < this.state.numberOfGoals) {
      alert("Liczba celów w przestrzeni bramkowej nie może być większa niż liczba pól w tym obszarze.");
      return;
    }

    if (2 * this.state.numberOfPlayersPerTeam > this.state.width * this.state.height) {
      alert("Liczba agentów w obu drużynach nie może przekraczać liczby pól na planszy.");
      return;
    }

    const state = this.state;
    for (const key in state) {
      if (state.hasOwnProperty(key)) {
        const val = state[key];
        if (!isNaN(val) && val < 0) {
          alert("Parametry nie mogą przyjmować wartości ujemnych.");
          return;
        }
      }
    }

    if (this.state.shamPieceProbability > 100) {
      alert("Prawdopodobieństwo musi być liczbą z przedziału między 0 a 100.");
      return;
    }

    fetch(`${API_URL}/configuration`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(this.state),
    }).then(res => {
      if (res.ok) {
        alert("Zmiany zostały zapisane.");
      } else {
        error(res);
      }
    }, error);
  }

  render() {
    return (
      <form>
        <h1 className="text-center">Konfiguracja gry</h1>

        <div className="form-row">
          <legend>Informacje o CS</legend>
          {CustomFieldset({
            id: "IP",
            type: "string",
            value: this.state.csIP,
            onChange: e => this.setState({ csIP: e.target.value }),
          })}
          {CustomFieldset({
            id: "Port",
            value: this.state.csPort,
            onChange: e => this.setState({ csPort: e.target.value }),
          })}
        </div>

        <div className="form-row">
          <legend>Opóźnienie w wykonywaniu ruchów przez agenta </legend>
          {CustomFieldset({
            id: "movePenalty",
            label: "Kara za ruch",
            value: this.state.movePenalty,
            onChange: e => this.setState({ movePenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "askPenalty",
            label: "Kara za prośbę komunikacji",
            value: this.state.askPenalty,
            onChange: e => this.setState({ askPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "discoverPenalty",
            label: "Kara za akcję Discovery",
            value: this.state.discoverPenalty,
            onChange: e => this.setState({ discoverPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "putPenalty",
            label: "Kara za odłożenie fragmentu",
            value: this.state.putPenalty,
            onChange: e => this.setState({ putPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "checkForShamePenalty",
            label: "Kara za sprawdzenie fragmentu",
            value: this.state.checkPenalty,
            onChange: e => this.setState({ checkPenalty: e.target.value }),
          })}

          {CustomFieldset({
            id: "responsePenalty",
            label: "Kara za odpowiedź",
            value: this.state.responsePenalty,
            onChange: e => this.setState({ responsePenalty: e.target.value }),
          })}
        </div>

        <div className="form-row">
          <legend>Rozmiar planszy</legend>
          {CustomFieldset({
            id: "height",
            label: "Wysokość",
            min: 1,
            value: this.state.height,
            onChange: e => this.setState({ height: e.target.value }),
          })}
          {CustomFieldset({
            id: "width",
            label: "Szerokość",
            min: 1,
            value: this.state.width,
            onChange: e => this.setState({ width: e.target.value }),
          })}
        </div>

        <div className="form-row">
          <legend>Rozmiar pola bramkowego</legend>
          {CustomFieldset({
            id: "goalAreaHeight",
            label: "Wysokość",
            min: 1,
            value: this.state.goalAreaHeight,
            onChange: e => this.setState({ goalAreaHeight: e.target.value }),
          })}
        </div>

        <fieldset className="form-group">
          <label>Ilość celów w polu bramkowym </label>
          <input
            className="form-control"
            type="number"
            min="1"
            name="numberOfGoals"
            value={this.state.numberOfGoals}
            onChange={e => this.setState({ numberOfGoals: e.target.value })}
          />
        </fieldset>

        <fieldset className="form-group">
          <label>Liczba agentów w każdej drużynie </label>
          <input
            className="form-control"
            type="number"
            min="1"
            name="numberOfPlayersPerTeam"
            value={this.state.numberOfPlayersPerTeam}
            onChange={e => this.setState({ numberOfPlayersPerTeam: e.target.value })}
          />
        </fieldset>

        <fieldset className="form-group">
          <label>Maksymalna liczba kawałków na planszy </label>
          <input
            className="form-control"
            type="number"
            min="1"
            name="numberOfPiecesOnBoard"
            value={this.state.numberOfPiecesOnBoard}
            onChange={e => this.setState({ numberOfPiecesOnBoard: e.target.value })}
          />
        </fieldset>

        <fieldset className="form-group">
          <label>Prawdopodobieństwo, że pojawiający się fragment jest fragmentem ﬁkcyjnym (%) </label>
          <input
            className="form-control"
            type="number"
            min="0"
            max="100"
            name="shamPieceProbability"
            value={this.state.shamPieceProbability}
            onChange={e => this.setState({ shamPieceProbability: e.target.value })}
          />
        </fieldset>

        <input className="btn btn-primary btn-block w-100" type="submit" value="Zapisz" onClick={this.sendData} />
      </form>
    );
  }
}
