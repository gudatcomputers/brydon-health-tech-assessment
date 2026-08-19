import { useState, type FormEvent } from "react";
import { login } from "../api/auth";

export function LoginPage() {
  const [step, setStep] = useState<1 | 2>(1);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [matchedSubdomains, setMatchedSubdomains] = useState<string[]>([]);
  const [selectedSubdomain, setSelectedSubdomain] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function handleContinue(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStep(2);
  }

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const outcome = await login(username, password, matchedSubdomains.length > 0 ? selectedSubdomain : undefined);

      if (outcome.status === "multiple") {
        setMatchedSubdomains(outcome.subdomains);
        setSelectedSubdomain(outcome.subdomains[0]);
        return;
      }

      // Patient-portal never hosts the session itself — hand the browser off
      // to the tenant's own client, which owns everything from here on
      // (welcome page, logout, whatever else it wants). Cross-origin, so a
      // real navigation, not client-side routing. Token goes in the URL
      // fragment (not a query string) so it's never sent to a server or
      // logged.
      window.location.href = `${outcome.clientOrigin}/handoff#token=${encodeURIComponent(outcome.token)}`;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong, please try again");
    } finally {
      setIsSubmitting(false);
    }
  }

  function handleBack() {
    setStep(1);
    setPassword("");
    setMatchedSubdomains([]);
    setError(null);
  }

  if (step === 1) {
    return (
      <section className="auth-page">
        <h1>Sign in</h1>
        <form onSubmit={handleContinue}>
          <label htmlFor="username">Username</label>
          <input
            id="username"
            name="username"
            autoComplete="username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            required
            autoFocus
          />

          <button type="submit">Continue</button>
        </form>
      </section>
    );
  }

  return (
    <section className="auth-page">
      <h1>Sign in</h1>
      <form onSubmit={handleLogin}>
        <p className="auth-identity">
          Signing in as <strong>{username}</strong>
        </p>

        {matchedSubdomains.length > 1 && (
          <fieldset className="auth-subdomain-picker">
            <legend>Which provider?</legend>
            {matchedSubdomains.map((subdomain) => (
              <label key={subdomain} className="auth-subdomain-option">
                <input
                  type="radio"
                  name="subdomain"
                  value={subdomain}
                  checked={selectedSubdomain === subdomain}
                  onChange={() => setSelectedSubdomain(subdomain)}
                />
                {subdomain}
              </label>
            ))}
          </fieldset>
        )}

        <label htmlFor="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
          autoFocus
        />

        {error && (
          <p role="alert" className="auth-error">
            {error}
          </p>
        )}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Signing in…" : "Sign in"}
        </button>
        <button type="button" className="auth-back" onClick={handleBack} disabled={isSubmitting}>
          Back
        </button>
      </form>
    </section>
  );
}
