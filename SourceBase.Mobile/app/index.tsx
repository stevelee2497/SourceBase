import { Redirect } from "expo-router";

// Entry point — the route guard in _layout handles auth redirection,
// but we default authenticated users into the tabs.
export default function Index() {
  return <Redirect href="/(tabs)" />;
}
