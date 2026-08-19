import { useEffect, useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

// Landed on from patient-portal after it proxies a login to this tenant's own
// server — the token in the URL fragment has already been verified there,
// this page just needs to pick it up and start a normal session with it.
// Fragment, not a query string, so the token is never sent to a server or
// logged.
export function HandoffPage() {
  const { acceptToken, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  // Captured once via the lazy initializer, not read fresh on every render.
  // navigate("/welcome") below updates window.location via the History API
  // immediately, which wipes the #token fragment — if this were recomputed
  // from window.location.hash on a later render (e.g. one more render of
  // this component before it actually unmounts, which React Router does not
  // guarantee won't happen), it would come back null and the !token branch
  // below would redirect to /login, overriding the /welcome navigation
  // already in flight. That's a real bug this project hit once already.
  const [token] = useState(() => new URLSearchParams(window.location.hash.slice(1)).get("token"));

  useEffect(() => {
    if (token) {
      acceptToken(token);
    }
  }, [token, acceptToken]);

  // Separate effect, gated on isAuthenticated rather than fired right after
  // acceptToken() above — navigating in the same effect that just called the
  // state setter raced ProtectedRoute's check against a context value that
  // hadn't re-rendered yet, bouncing back to /login even though the token
  // was already stored correctly. Waiting for isAuthenticated to actually
  // flip true guarantees the context has caught up first.
  useEffect(() => {
    if (isAuthenticated) {
      navigate("/welcome", { replace: true });
    }
  }, [isAuthenticated, navigate]);

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  return null;
}
