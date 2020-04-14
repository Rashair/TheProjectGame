import React, { Component } from "react";
import { Route } from "react-router";
import { Layout } from "./components/Layout";
import { Configuration } from "./components/Configuration";
import { Start } from "./components/Start";
import Notifications from "react-notify-toast";

import "./custom.css";

export default class App extends Component {
  render() {
    return (
      <Layout>
        <Notifications />
        <Route exact path="/" component={Start} />
        <Route exact path="/configuration" component={Configuration} />
      </Layout>
    );
  }
}
