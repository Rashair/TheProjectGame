import { notify } from "react-notify-toast";

export const error = (e) => {
  notify.show("Coś poszło nie tak, spróbuj ponownie", "error", 3000);
  console.log(`Error: ${e}`);
};
