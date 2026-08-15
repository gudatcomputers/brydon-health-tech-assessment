import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

export function WelcomePage() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  return (
    <section id="welcome">
      <h1>Welcome</h1>
      <p>You're signed in.</p>
      <button type="button" onClick={handleLogout}>
        Log out
      </button>
    </section>
  );
}
