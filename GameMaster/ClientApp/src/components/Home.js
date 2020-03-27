import React, { Component } from "react";

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

export class Home extends Component {
  constructor(props, context) {
    super(props, context);
    this.state = {
      ip: "",
      port: 0,
      movePenalty: 0,
      askPenalty: 0,
      discoverPenalty: 0,
      putPenalty: 0,
      checkPenalty: 0,
      responsePenalty: 0,
      boardX: 0,
      boardY: 0,
      goalAreaHeight: 0,
      numberOfGoals: 0,
      numberOfTeamPlayers: 0,
      shamPieceProbability: 0,
      maximumNumberOfPiecesOnBoard: 0,
    };

    this.sendData = this.sendData.bind(this);
  }

  componentDidMount() {
    fetch("/Configuration")
      .then(
        res => {
          if (res.ok) {
            return res.json();
          }
          throw res.statusText;
        },
        e => console.log(e)
      )
      .then(json => {
        this.setState(
          {
            ip: json.csIP,
            port: json.csPort,
            movePenalty: json.movePenalty,
            askPenalty: json.askPenalty,
            discoverPenalty: json.discoverPenalty,
            putPenalty: json.putPenalty,
            checkPenalty: json.checkPenalty,
            responsePenalty: json.responsePenalty,
            boardX: json.width,
            boardY: json.height,
            goalAreaHeight: json.goalAreaHeight,
            numberOfGoals: json.numberOfGoals,
            numberOfPieces: json.numberOfPieces,
            numberOfPlayersPerTeam: json.numberOfPlayersPerTeam,
            shamPieceProbability: json.shamPieceProbability,
            maximumNumberOfPiecesOnBoard: json.maximumNumberOfPiecesOnBoard,
          },
          e => console.log(e)
        );
      });
  }

  sendData(event) {
    event.preventDefault();
    var xhr = new XMLHttpRequest();

    xhr.onreadystatechange = function() {
      if (xhr.readyState === XMLHttpRequest.DONE) {
        const status = xhr.status;
        if (status === 200) {
          alert("Zmiany zostały zapisane.");
        } else {
          alert("Coś poszło nie tak, spróbuj ponownie.");
        }
      }
    };

    if (this.state.goalAreaHeight * this.state.boardX < this.state.numberOfGoals) {
      alert("Liczba celów w przestrzeni bramkowej nie może być większa niż liczba pól w tym obszarze.");
      return;
    }

    if (2 * this.state.numberOfTeamPlayers > this.state.boardX * this.state.boardY) {
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

    xhr.open("POST", "/Configuration", true);

    var data = new FormData();
    data.append("CsIP", this.state.ip);
    data.append("CsPort", this.state.port);
    data.append("MovePenalty", this.state.movePenalty);
    data.append("AskPenalty", this.state.askPenalty);
    data.append("DiscoverPenalty", this.state.discoverPenalty);
    data.append("PutPenalty", this.state.putPenalty);
    data.append("CheckPenalty", this.state.checkPenalty);
    data.append("ResponsePenalty", this.state.responsePenalty);
    data.append("Width", this.state.boardX);
    data.append("Height", this.state.boardY);
    data.append("GoalAreaHeight", this.state.goalAreaHeight);
    data.append("NumberOfGoals", this.state.numberOfGoals);
    data.append("NumberOfPlayersPerTeam", this.state.numberOfPlayersPerTeam);
    data.append("MaximumNumberOfPiecesOnBoard", this.state.maximumNumberOfPiecesOnBoard);
    data.append("ShamPieceProbability", this.state.shamPieceProbability);
    xhr.send(data);
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
            value: this.state.ip,
            onChange: e => this.setState({ ip: e.target.value }),
          })}
          {CustomFieldset({
            id: "Port",
            value: this.state.port,
            onChange: e => this.setState({ port: e.target.value }),
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
            id: "boardY",
            label: "Wysokość",
            min: 1,
            value: this.state.boardY,
            onChange: e => this.setState({ boardY: e.target.value }),
          })}
          {CustomFieldset({
            id: "boardX",
            label: "Szerokość",
            min: 1,
            value: this.state.boardX,
            onChange: e => this.setState({ boardX: e.target.value }),
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
            name="maximumNumberOfPiecesOnBoard"
            value={this.state.maximumNumberOfPiecesOnBoard}
            onChange={e => this.setState({ maximumNumberOfPiecesOnBoard: e.target.value })}
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
