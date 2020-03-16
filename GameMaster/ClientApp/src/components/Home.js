import React, { Component } from 'react';

export class Home extends Component {
  displayName = Home.name

  render() {
    return (
      <form>
            <h1>Zmień domyślną konfigurację gry</h1>

            <fieldset>
                <label>Informacje o CS:</label>
                <fieldset>
                    <label>IP: </label>
                    <input type="string" />
                </fieldset>
                <fieldset>
                    <label>Port: </label>
                    <input type="number" />
                </fieldset>
            </fieldset>

            <fieldset>
                <label>Opóźnienie w wykonywaniu ruchów przez agenta: </label>
                <fieldset>
                    <label>Kara za ruch: </label>
                    <input type="number" />
                </fieldset><fieldset>
                    <label>Kara za prośbę komunikacji: </label>
                    <input type="number" />
                </fieldset>
                <fieldset>
                    <label>Kara za akcję Discovery: </label>
                    <input type="number" />
                </fieldset>
                <fieldset>
                    <label>Kara za odłożenie fragmentu: </label>
                    <input type="number" />
                </fieldset>
                <fieldset>
                    <label>Kara za sprawdzenie fragmentu: </label>
                    <input type="number" />
                </fieldset>
                <fieldset>
                    <label>Kara za odpowiedź: </label>
                    <input type="number" />
                </fieldset>
            </fieldset>

            <fieldset>
                <label>Rozmiar planszy:</label>
                <fieldset>
                    <label>Wysokość: </label>
                    <input type="number" />
                </fieldset>
                <fieldset>
                    <label>Szerokość: </label>
                    <input type="number" />
                </fieldset>
            </fieldset>

            <fieldset>
                <label>Rozmiar pola bramkowego:</label>
                <fieldset>
                    <label>Wysokość: </label>
                    <input type="number" />
                </fieldset>
            </fieldset>

            <fieldset>
                <label>Maksymalna liczba fragmentów na planszy: </label>
                <input type="number" />
            </fieldset>

            <fieldset>
                <label>Ilość celów w polu bramkowym: </label>
                <input type="number" />
            </fieldset>

            <fieldset>
                <label>Prawdopodobieństwo, że pojawiający się fragment jest fragmentem ﬁkcyjnym: </label>
                <label>0.</label>
                <input type="number" />
            </fieldset>

            <fieldset>
                <label>Częstotliwość generowania nowego fragmentu na planszy: </label>
                <label>1/</label>
                <input type="number" /><label>ms</label>
            </fieldset>
            
            <fieldset>
                <label>Ilość graczy: </label>
                <input type="number" />
            </fieldset>

            <input type="submit" value="Zapisz" />
        </form>
    );
  }
}
