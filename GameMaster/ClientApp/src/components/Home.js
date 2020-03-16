import React, { Component } from 'react';

export class Home extends Component {
  displayName = Home.name

  render() {
    return (
      <form>
            <h1>Zmień domyślną konfigurację gry</h1>
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
                <label>Ilość graczy: </label>
                <input type="number" />
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
                <label>Rozmiar pola bramkowego:</label>
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
                <label>Opóźnienie w wykonywaniu ruchów przez agenta: </label>
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
        </form>
    );
  }
}
