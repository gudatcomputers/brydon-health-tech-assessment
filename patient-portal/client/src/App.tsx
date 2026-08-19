import { Route, Routes } from "react-router-dom";
import { LoginPage } from "./pages/LoginPage";
import "./App.css";

function App() {
  return (
    <Routes>
      <Route path="/" element={<LoginPage />} />
      <Route path="/login" element={<LoginPage />} />
    </Routes>
  );
}

export default App;
