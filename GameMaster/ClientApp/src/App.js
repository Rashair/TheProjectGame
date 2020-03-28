import React, { Component } from "react";
import { Route } from "react-router";
import { Layout } from "./components/Layout";
import { Home } from "./components/Home";
import { Start } from "./components/Start";

import "./custom.css";

export default class App extends Component {
  render() {
    return (
      <Layout>
        <Route exact path="/" component={Home} />
        <Route exact path="/start" component={Start} />
      </Layout>
    );
  }
}
