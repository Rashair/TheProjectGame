import React, { Component } from "react";
import { error } from "../helpers/errors";
import { API_URL } from "../helpers/constants";
import { notify } from "react-notify-toast";

const CustomFieldset = ({ id, label, type = "number", min = 0, value, onChange, regex, title }) => {
  return (
    <fieldset className="form-group col-md-4 mb-4 pl-3 ">
      <label className="ml-1" htmlFor={id}>
        {label ? label : id}
      </label>
      <input
        className="form-control"
        type={type}
        id={id}
        min={min}
        value={value}
        onChange={onChange}
        pattern={regex}
        title={title}
        required="required"
      />
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
      responsePenalty: 0,
      discoverPenalty: 0,
      pickPenalty: 0,
      checkPenalty: 0,
      putPenalty: 0,
      destroyPenalty: 0,
      width: 0,
      height: 0,
      goalAreaHeight: 0,
      numberOfGoals: 0,
      numberOfPlayersPerTeam: 0,
      shamPieceProbability: 0.0,
      numberOfPiecesOnBoard: 0
    };

    this.sendData = this.sendData.bind(this);
  }

  componentDidMount() {
    fetch(`${API_URL}/configuration`)
      .then((res) => {
        if (res.ok) {
          return res.json();
        } else {
          error(res);
        }
      }, error)
      .then((json) => this.setState(json), error);
  }

  sendData(event) {
    event.preventDefault();

    if (this.state.csPort)
      if (this.state.goalAreaHeight * this.state.width < this.state.numberOfGoals) {
        notify.show(
          "Liczba celów w przestrzeni bramkowej nie może być większa niż liczba pól w tym obszarze",
          "warning",
          3000
        );
        return;
      }

    if (2 * this.state.numberOfPlayersPerTeam > this.state.width * this.state.height) {
      notify.show("Liczba agentów w obu drużynach nie może przekraczać liczby pól na planszy", "warning", 3000);
      return;
    }

    const state = this.state;
    for (const key in state) {
      if (state.hasOwnProperty(key)) {
        const val = state[key];
        if (!isNaN(val) && val < 0) {
          notify.show("Parametry nie mogą przyjmować wartości ujemnych", "warning", 3000);
          return;
        }
      }
    }

    if (this.state.shamPieceProbability > 1) {
      notify.show("Prawdopodobieństwo musi być liczbą z przedziału między 0 a 1.", "warning", 3000);
      return;
    }

    fetch(`${API_URL}/configuration`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(this.state),
    }).then((res) => {
      if (res.ok) {
        notify.show("Konfiguracja zapisana!", "success", 3000);
        this.props.history.push("/");
      } else {
        error(res);
      }
    }, error);
  }

  render() {
    return (
      <form onSubmit={(e) => this.sendData(e)}>
        <div className="form-row">
          <legend>Informacje o CS</legend>
          {CustomFieldset({
            id: "IP",
            type: "string",
            value: this.state.csIP,
            regex:
              "(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
            title: "Wprowadź poprawny adres IP",
            onChange: (e) => this.setState({ csIP: e.target.value }),
          })}
          {CustomFieldset({
            id: "Port",
            min: 1024,
            value: this.state.csPort,
            onChange: (e) => this.setState({ csPort: e.target.value }),
          })}
        </div>

        <div className="form-row">
          <legend>Opóźnienie w wykonywaniu akcji przez agenta </legend>
          {CustomFieldset({
            id: "movePenalty",
            label: "Kara za ruch",
            value: this.state.movePenalty,
            onChange: (e) => this.setState({ movePenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "askPenalty",
            label: "Kara za prośbę o komunikację",
            value: this.state.askPenalty,
            onChange: (e) => this.setState({ askPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "responsePenalty",
            label: "Kara za odpowiedź na komunikację",
            value: this.state.responsePenalty,
            onChange: (e) => this.setState({ responsePenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "discoverPenalty",
            label: "Kara za akcję discovery",
            value: this.state.discoverPenalty,
            onChange: (e) => this.setState({ discoverPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "pickPenalty",
            label: "Kara za podniesienie fragmentu",
            value: this.state.pickPenalty,
            onChange: (e) => this.setState({ pickPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "checkPenalty",
            label: "Kara za sprawdzenie fragmentu",
            value: this.state.checkPenalty,
            onChange: (e) => this.setState({ checkPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "putPenalty",
            label: "Kara za odłożenie fragmentu",
            value: this.state.putPenalty,
            onChange: (e) => this.setState({ putPenalty: e.target.value }),
          })}
          {CustomFieldset({
            id: "destroyPenalty",
            label: "Kara za zniszczenie fragmentu",
            value: this.state.destroyPenalty,
            onChange: (e) => this.setState({ destroyPenalty: e.target.value }),
          })}
        </div>

        <div className="form-row">
          <legend>Rozmiar planszy</legend>
          {CustomFieldset({
            id: "height",
            label: "Wysokość",
            min: 1,
            value: this.state.height,
            onChange: (e) => this.setState({ height: e.target.value }),
          })}
          {CustomFieldset({
            id: "width",
            label: "Szerokość",
            min: 1,
            value: this.state.width,
            onChange: (e) => this.setState({ width: e.target.value }),
          })}
        </div>

        <div className="form-row">
          <legend>Pole bramkowe</legend>
          {CustomFieldset({
            id: "goalAreaHeight",
            label: "Wysokość",
            min: 1,
            value: this.state.goalAreaHeight,
            onChange: (e) => this.setState({ goalAreaHeight: e.target.value }),
          })}
          {CustomFieldset({
            id: "numberOfGoals",
            label: "Ilość celów",
            min: 1,
            value: this.state.numberOfGoals,
            onChange: (e) => this.setState({ numberOfGoals: e.target.value }),
          })}
        </div>

        <fieldset className="form-group">
          <label>Liczba agentów w każdej drużynie </label>
          <input
            className="form-control"
            type="number"
            min="1"
            name="numberOfPlayersPerTeam"
            value={this.state.numberOfPlayersPerTeam}
            onChange={(e) => this.setState({ numberOfPlayersPerTeam: e.target.value })}
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
            onChange={(e) => this.setState({ numberOfPiecesOnBoard: e.target.value })}
          />
        </fieldset>

        <fieldset className="form-group">
          <label>Prawdopodobieństwo, że pojawiający się fragment jest fragmentem ﬁkcyjnym (0-1) </label>
          <input
            className="form-control"
            type="number"
            min="0"
            max="1"
            step="0.01"
            name="shamPieceProbability"
            value={this.state.shamPieceProbability}
            onChange={(e) => this.setState({ shamPieceProbability: e.target.value })}
          />
            </fieldset>

        <input className="btn btn-primary btn-block w-100" type="submit" value="Zapisz" />
      </form>
    );
  }
}
