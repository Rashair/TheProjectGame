import React, { Component } from 'react';
import gameConfigData from './gameConfigData';

export class Home extends Component {
    constructor(props, context) {
        super(props, context)
        this.state = {
            ip: gameConfigData.CsIP,
            port: gameConfigData.CsPort,
            movePenalty: gameConfigData.movePenalty,
            askPenalty: gameConfigData.askPenalty,
            discoveryPenalty: gameConfigData.discoveryPenalty,
            putPenalty: gameConfigData.putPenalty,
            checkForShamPenalty: gameConfigData.checkForShamPenalty,
            responsePenalty: gameConfigData.responsePenalty,
            boardX: gameConfigData.boardX,
            boardY: gameConfigData.boardY,
            goalAreaHeight: gameConfigData.goalAreaHeight,
            numberOfGoals: gameConfigData.numberOfGoals,
            numberOfPieces: gameConfigData.numberOfPieces,
            numberOfTeamPlayers: gameConfigData.numberOfPlayersPerTeam,
            shamPieceProbability: gameConfigData.shamPieceProbability,
            maximumNumberOfPiecesOnBoard: gameConfigData.maximumNumberOfPiecesOnBoard,
        }

        this.sendData = this.sendData.bind(this);
    }

    sendData(event) {
        event.preventDefault();
        var xhr = new XMLHttpRequest();

        xhr.onreadystatechange = function () {
            if (xhr.readyState === XMLHttpRequest.DONE) {
                var status = xhr.status;
                if (this.readyState == 4 && this.status == 200) {
                    alert('Zmiany zostały zapisane.');
                } else {
                    alert('Coś poszło nie tak, spróbuj ponownie.');
                }
            }
        };

        let readyToSend = true;
        if (this.state.goalAreaHeight * this.state.boardX < this.state.numberOfGoals) {
            alert('Liczba celów w przestrzeni bramkowej nie może być większa niż liczba pól w tym obszarze.');
            readyToSend = false;
        }

        if (2 * this.state.numberOfTeamPlayers > (this.state.boardX * this.state.boardY)) {
            alert('Liczba agentów w obu drużynach nie może przekraczać liczby pól na planszy.');
            readyToSend = false;
        }

        if (this.state.movePenalty < 0 || this.state.askPenalty < 0 || this.state.discoveryPenalty < 0 || this.state.putPenalty < 0 ||
            this.state.checkForShamPenalty < 0 || this.state.responsePenalty < 0) {
            alert('Kara musi być wartością nieujemną.');
            readyToSend = false;
        }

        if (this.state.boardX < 0 || this.state.boardY < 0 || this.state.goalAreaHeight < 0 || this.state.numberOfTeamPlayers < 0
            || this.state.numberOfGoals < 0 || this.state.numberOfPieces < 0 || this.state.shamPieceProbability < 0) {
            alert('Parametry nie mogą przyjmować wartości ujemnych.');
            readyToSend = false;
        }

        if (this.state.shamPieceProbability < 0 || this.state.shamPieceProbability >= 1) {
            alert('Prawdopodobieństwo musi być liczbą z przedziału między 0 a 1.');
            readyToSend = false;
        }

        if (readyToSend) {
            xhr.open('POST', 'Configuration', true);

            var data = new FormData();
            data.append('CsIP', this.state.ip);
            data.append('CsPort', this.state.port);
            data.append('MovePenalty', this.state.movePenalty);
            data.append('AskPenalty', this.state.askPenalty);
            data.append('DiscoverPenalty', this.state.discoveryPenalty);
            data.append('PutPenalty', this.state.putPenalty);
            data.append('CheckPenalty', this.state.checkForShamPenalty);
            data.append('ResponsePenalty', this.state.responsePenalty);
            data.append('Width', this.state.boardX);
            data.append('Height', this.state.boardY);
            data.append('GoalAreaHeight', this.state.goalAreaHeight);
            data.append('NumberOfGoals', this.state.numberOfGoals);
            data.append('NumberOfPieces', this.state.numberOfPieces);
            data.append('NumberOfPlayersPerTeam', this.state.numberOfTeamPlayers);
            data.append('MaximumNumberOfPiecesOnBoard', this.state.maximumNumberOfPiecesOnBoard);
            data.append('ShamPieceProbability', this.state.shamPieceProbability);
            xhr.send(data);
        }
    }

    render() {
        return (
            <form >
                <h1>Zmień domyślną konfigurację gry</h1>

                <fieldset>
                    <label>Informacje o CS:</label>
                    <fieldset>
                        <label>IP: </label>
                        <input type="string" name="ip" value={this.state.ip}
                            onChange={e => this.setState({ ip: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Port: </label>
                        <input type="number" min="0" name="port" value={this.state.port}
                            onChange={e => this.setState({ port: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Opóźnienie w wykonywaniu ruchów przez agenta: </label>
                    <fieldset>
                        <label>Kara za ruch: </label>
                        <input type="number" min="0" name="movePenalty" value={this.state.movePenalty}
                            onChange={e => this.setState({ movePenalty: e.target.value })} />
                    </fieldset><fieldset>
                        <label>Kara za prośbę komunikacji: </label>
                        <input type="number" min="0" name="askPenalty" value={this.state.askPenalty}
                            onChange={e => this.setState({ askPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za akcję Discovery: </label>
                        <input type="number" min="0" name="discoveryPenalty" value={this.state.discoveryPenalty}
                            onChange={e => this.setState({ discoveryPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za odłożenie fragmentu: </label>
                        <input type="number" min="0" name="putPenalty" value={this.state.putPenalty}
                            onChange={e => this.setState({ putPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za sprawdzenie fragmentu: </label>
                        <input type="number" min="0" name="checkForShamePenalty" value={this.state.checkForShamPenalty}
                            onChange={e => this.setState({ checkForShamPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za odpowiedź: </label>
                        <input type="number" min="0" name="responsePenalty" value={this.state.responsePenalty}
                            onChange={e => this.setState({ responsePenalty: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Rozmiar planszy:</label>
                    <fieldset>
                        <label>Wysokość: </label>
                        <input type="number" min="1" name="boardY" value={this.state.boardY}
                            onChange={e => this.setState({ boardY: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Szerokość: </label>
                        <input type="number" min="1" name="boardX" value={this.state.boardX}
                            onChange={e => this.setState({ boardX: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Rozmiar pola bramkowego:</label>
                    <fieldset>
                        <label>Wysokość: </label>
                        <input type="number" min="1" name="goalAreaHeight" value={this.state.goalAreaHeight}
                            onChange={e => this.setState({ goalAreaHeight: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Maksymalna liczba fragmentów na planszy: </label>
                    <input type="number" min="1" name="numberOfPieces" value={this.state.numberOfPieces}
                        onChange={e => this.setState({ numberOfPieces: e.target.value })} />
                </fieldset>

                <fieldset>
                    <label>Ilość celów w polu bramkowym: </label>
                    <input type="number" min="1" name="numberOfGoals" value={this.state.numberOfGoals}
                        onChange={e => this.setState({ numberOfGoals: e.target.value })} />
                </fieldset>

                <fieldset>
                    <label>Ilość agentów w drużynie: </label>
                    <input type="number" min="1" name="numberOfTeamPlayers" value={this.state.numberOfTeamPlayers}
                        onChange={e => this.setState({ numberOfTeamPlayers: e.target.value })} />

                </fieldset>

                <fieldset>
                    <label>Maksymalna liczba kawałków na planszy: </label>
                    <input type="number" min="1" name="maximumNumberOfPiecesOnBoard" value={this.state.maximumNumberOfPiecesOnBoard}
                        onChange={e => this.setState({ maximumNumberOfPiecesOnBoard: e.target.value })} />

                </fieldset>

                <fieldset>
                    <label>Prawdopodobieństwo, że pojawiający się fragment jest fragmentem ﬁkcyjnym: </label>
                    <input type="number" min="0" max="1" name="shamPieceProbability" value={this.state.shamPieceProbability}
                        onChange={e => this.setState({ shamPieceProbability: e.target.value })} />
                </fieldset>

                <input type="submit" value="Zapisz" onClick={this.sendData} />
            </form>
        );
    }
}
