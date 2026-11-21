"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import type { ApiError } from "@/lib/api";
import styles from "./login.module.css";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const MIN_PASSWORD_LENGTH = 8;

interface FieldErrors {
  email?: string;
  password?: string;
}

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function validate(): FieldErrors {
    const errors: FieldErrors = {};
    if (!email.trim()) errors.email = "Введите email.";
    else if (!EMAIL_RE.test(email.trim())) errors.email = "Некорректный формат email.";
    if (!password) errors.password = "Введите пароль.";
    else if (password.length < MIN_PASSWORD_LENGTH)
      errors.password = `Пароль должен быть не короче ${MIN_PASSWORD_LENGTH} символов.`;
    return errors;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);
    const errors = validate();
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSubmitting(true);
    try {
      await login(email.trim(), password);
      router.push("/profile");
      router.refresh();
    } catch (err) {
      const apiErr = err as ApiError;
      if (apiErr.kind === "network") {
        setFormError("Сервер недоступен. Проверьте соединение и попробуйте позже.");
      } else if (apiErr.status === 401) {
        setFormError("Неверный email или пароль.");
      } else if (apiErr.status === 400) {
        setFormError(apiErr.message || "Некорректный запрос.");
      } else {
        setFormError(apiErr.message || "Не удалось войти. Попробуйте позже.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.title}>Вход</h1>
        <p className={styles.subtitle}>Войдите, чтобы продолжить обучение.</p>

        {formError && <div className={styles.formError}>{formError}</div>}

        <form className={styles.form} onSubmit={handleSubmit} noValidate>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              className={styles.input}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              aria-invalid={Boolean(fieldErrors.email)}
              disabled={submitting}
            />
            {fieldErrors.email && <span className={styles.fieldError}>{fieldErrors.email}</span>}
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="password">Пароль</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              className={styles.input}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              aria-invalid={Boolean(fieldErrors.password)}
              disabled={submitting}
            />
            {fieldErrors.password && <span className={styles.fieldError}>{fieldErrors.password}</span>}
          </div>

          <button className={styles.submit} type="submit" disabled={submitting}>
            {submitting ? "Вход…" : "Войти"}
          </button>
        </form>

        <div className={styles.footer}>
          Нет аккаунта?{" "}
          <Link href="/register" className={styles.link}>Зарегистрироваться</Link>
        </div>
      </div>
    </div>
  );
}