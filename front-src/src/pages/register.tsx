import { useState } from "react";
import { Link, useNavigate } from "react-router";
import { ActivityIcon } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Field, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";

export function RegisterPage() {
  const { signUp } = useAuth();
  const navigate = useNavigate();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);
    setBusy(true);
    try {
      await signUp(email.trim(), password, firstName.trim() || undefined, lastName.trim() || undefined);
      navigate("/dashboard", { replace: true });
    } catch (error) {
      const message = error instanceof ApiError ? error.message : "Could not create the account.";
      setFormError(message);
      toast.error(message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <div className="flex items-center gap-2">
            <span className="flex size-8 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <ActivityIcon data-icon="inline-start" className="size-4" />
            </span>
            <CardTitle>KeepTabs</CardTitle>
          </div>
          <CardDescription>Create an account to start monitoring.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <div className="flex flex-col gap-4 sm:flex-row">
                <Field>
                  <FieldLabel htmlFor="register-first">First name</FieldLabel>
                  <Input
                    id="register-first"
                    autoComplete="given-name"
                    value={firstName}
                    onChange={(event) => setFirstName(event.target.value)}
                    placeholder="Ada"
                  />
                </Field>
                <Field>
                  <FieldLabel htmlFor="register-last">Last name</FieldLabel>
                  <Input
                    id="register-last"
                    autoComplete="family-name"
                    value={lastName}
                    onChange={(event) => setLastName(event.target.value)}
                    placeholder="Lovelace"
                  />
                </Field>
              </div>
              <Field>
                <FieldLabel htmlFor="register-email">Email</FieldLabel>
                <Input
                  id="register-email"
                  type="email"
                  autoComplete="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder="you@example.com"
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="register-password">Password</FieldLabel>
                <Input
                  id="register-password"
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                />
              </Field>
              {formError && (
                <Field data-invalid>
                  <FieldError>{formError}</FieldError>
                </Field>
              )}
              <Field>
                <Button type="submit" disabled={busy} className="w-full">
                  {busy && <Spinner data-icon="inline-start" />}
                  Create account
                </Button>
              </Field>
            </FieldGroup>
          </form>
          <p className="mt-4 text-center text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link to="/login" className="font-medium text-foreground underline-offset-4 hover:underline">
              Sign in
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
