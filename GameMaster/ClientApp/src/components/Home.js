﻿import React, { Component } from 'react';
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
            shamPieceProbability: gameConfigData.shamPieceProbability,
        }

        this.sendData = (event) => {
            event.preventDefault();
            var xhr = new XMLHttpRequest();

            xhr.onreadystatechange = function () {
                if (xhr.readyState === XMLHttpRequest.DONE) {
                    var status = xhr.status;
                    if (status === 0 || (200 >= status && status < 400)) {
                        alert('Zmiany zostały zapisane.');
                    } else {
                        alert('Coś poszło nie tak, spróbuj ponownie.');
                    }
                }
            };
            xhr.open('POST', 'Configuration', true);

            var data = new FormData();
            data.append('CsIP', this.state.ip);
            data.append('CsPort', this.state.port);
            data.append('movePenalty', this.state.movePenalty);
            data.append('askPenalty', this.state.askPenalty);
            data.append('discoveryPenalty', this.state.discoveryPenalty);
            data.append('putPenalty', this.state.putPenalty);
            data.append('checkForShamPenalty', this.state.checkForShamPenalty);
            data.append('responsePenalty', this.state.responsePenalty);
            data.append('boardX', this.state.boardX);
            data.append('boardY', this.state.boardY);
            data.append('goalAreaHeight', this.state.goalAreaHeight);
            data.append('numberOfGoals', this.state.numberOfGoals);
            data.append('numberOfPieces', this.state.numberOfPieces);
            data.append('shamPieceProbability', this.state.shamPieceProbability);
            xhr.send(data);
            
        }
    }
    displayName = Home.name

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
                        <input type="number" name="port" value={this.state.port}
                            onChange={e => this.setState({ port: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Opóźnienie w wykonywaniu ruchów przez agenta: </label>
                    <fieldset>
                        <label>Kara za ruch: </label>
                        <input type="number" name="movePenalty" value={this.state.movePenalty}
                            onChange={e => this.setState({ movePenalty: e.target.value })} />
                    </fieldset><fieldset>
                        <label>Kara za prośbę komunikacji: </label>
                        <input type="number" name="askPenalty" value={this.state.askPenalty}
                            onChange={e => this.setState({ askPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za akcję Discovery: </label>
                        <input type="number" name="discoveryPenalty" value={this.state.discoveryPenalty}
                            onChange={e => this.setState({ discoveryPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za odłożenie fragmentu: </label>
                        <input type="number" name="putPenalty" value={this.state.putPenalty}
                            onChange={e => this.setState({ putPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za sprawdzenie fragmentu: </label>
                        <input type="number" name="checkForShamePenalty" value={this.state.checkForShamPenalty}
                            onChange={e => this.setState({ checkForShamPenalty: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Kara za odpowiedź: </label>
                        <input type="number" name="responsePenalty" value={this.state.responsePenalty}
                            onChange={e => this.setState({ responsePenalty: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Rozmiar planszy:</label>
                    <fieldset>
                        <label>Wysokość: </label>
                        <input type="number" name="boardY" value={this.state.boardY}
                            onChange={e => this.setState({ boardY: e.target.value })} />
                    </fieldset>
                    <fieldset>
                        <label>Szerokość: </label>
                        <input type="number" name="boardX" value={this.state.boardX}
                            onChange={e => this.setState({ boardX: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Rozmiar pola bramkowego:</label>
                    <fieldset>
                        <label>Wysokość: </label>
                        <input type="number" name="goalAreaHeight" value={this.state.goalAreaHeight}
                            onChange={e => this.setState({ goalAreaHeight: e.target.value })} />
                    </fieldset>
                </fieldset>

                <fieldset>
                    <label>Maksymalna liczba fragmentów na planszy: </label>
                    <input type="number" name="numberOfPieces" value={this.state.numberOfPieces}
                        onChange={e => this.setState({ numberOfPieces: e.target.value })} />
                </fieldset>

                <fieldset>
                    <label>Ilość celów w polu bramkowym: </label>
                    <input type="number" name="numberOfGoals" value={this.state.numberOfGoals}
                        onChange={e => this.setState({ numberOfGoals: e.target.value })} />
                </fieldset>

                <fieldset>
                    <label>Prawdopodobieństwo, że pojawiający się fragment jest fragmentem ﬁkcyjnym: </label>
                    <input type="number" name="shamPieceProbability" value={this.state.shamPieceProbability}
                        onChange={e => this.setState({ shamPieceProbability: e.target.value })} />
                </fieldset>

                <input type="submit" value="Zapisz" onClick={this.sendData} />
            </form>
        );
    }
}
