"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/context/AuthContext";
import type { ApiError } from "@/lib/api";
import styles from "./register.module.css";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const MIN_PASSWORD_LENGTH = 8;
const MAX_NAME_LENGTH = 100;

interface FieldErrors {
  email?: string;
  password?: string;
  firstName?: string;
  lastName?: string;
}

function validatePassword(p: string): string | undefined {
  if (!p) return "Введите пароль.";
  if (p.length < MIN_PASSWORD_LENGTH)
    return `Пароль должен быть не короче ${MIN_PASSWORD_LENGTH} символов.`;
  if (!/[A-ZА-Я]/.test(p) || !/[a-zа-я]/.test(p) || !/\d/.test(p) || !/[^A-Za-zА-Яа-я0-9]/.test(p))
    return "Пароль должен содержать заглавную и строчную буквы, цифру и спецсимвол.";
  return undefined;
}

export default function RegisterPage() {
  const { register } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  function validate(): FieldErrors {
    const errors: FieldErrors = {};
    if (!email.trim()) errors.email = "Введите email.";
    else if (!EMAIL_RE.test(email.trim())) errors.email = "Некорректный формат email.";

    const pwErr = validatePassword(password);
    if (pwErr) errors.password = pwErr;

    if (!firstName.trim()) errors.firstName = "Введите имя.";
    else if (firstName.trim().length > MAX_NAME_LENGTH)
      errors.firstName = `Не длиннее ${MAX_NAME_LENGTH} символов.`;

    if (!lastName.trim()) errors.lastName = "Введите фамилию.";
    else if (lastName.trim().length > MAX_NAME_LENGTH)
      errors.lastName = `Не длиннее ${MAX_NAME_LENGTH} символов.`;

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
      await register({
        email: email.trim(),
        password,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
      });
      setSuccess(true);
    } catch (err) {
      const apiErr = err as ApiError;
      if (apiErr.kind === "network") {
        setFormError("Сервер недоступен. Проверьте соединение и попробуйте позже.");
      } else if (apiErr.status === 409) {
        setFormError("Пользователь с таким email уже существует.");
      } else if (apiErr.status === 400) {
        if (apiErr.fieldErrors && apiErr.fieldErrors.length > 0) {
          setFormError(apiErr.fieldErrors.join(" "));
        } else {
          setFormError(apiErr.message || "Проверьте введенные данные.");
        }
      } else {
        setFormError(apiErr.message || "Не удалось зарегистрироваться. Попробуйте позже.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (success) {
    return (
      <div className={styles.page}>
        <div className={styles.card}>
          <h1 className={styles.title}>Готово</h1>
          <p className={styles.subtitle}>Аккаунт создан. Теперь можно войти.</p>
          <Link href="/login" className={styles.submit} style={{ display: "block", textAlign: "center" }}>
            Перейти ко входу
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.title}>Регистрация</h1>
        <p className={styles.subtitle}>Создайте аккаунт, чтобы начать обучение.</p>

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

          <div className={styles.grid2}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="firstName">Имя</label>
              <input
                id="firstName"
                type="text"
                autoComplete="given-name"
                className={styles.input}
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                aria-invalid={Boolean(fieldErrors.firstName)}
                disabled={submitting}
              />
              {fieldErrors.firstName && <span className={styles.fieldError}>{fieldErrors.firstName}</span>}
            </div>

            <div className={styles.field}>
              <label className={styles.label} htmlFor="lastName">Фамилия</label>
              <input
                id="lastName"
                type="text"
                autoComplete="family-name"
                className={styles.input}
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                aria-invalid={Boolean(fieldErrors.lastName)}
                disabled={submitting}
              />
              {fieldErrors.lastName && <span className={styles.fieldError}>{fieldErrors.lastName}</span>}
            </div>
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="password">Пароль</label>
            <input
              id="password"
              type="password"
              autoComplete="new-password"
              className={styles.input}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              aria-invalid={Boolean(fieldErrors.password)}
              disabled={submitting}
            />
            {fieldErrors.password ? (
              <span className={styles.fieldError}>{fieldErrors.password}</span>
            ) : (
              <span className={styles.hint}>
                Минимум 8 символов, заглавная и строчная буквы, цифра и спецсимвол.
              </span>
            )}
          </div>

          <button className={styles.submit} type="submit" disabled={submitting}>
            {submitting ? "Создаем аккаунт…" : "Зарегистрироваться"}
          </button>
        </form>

        <div className={styles.footer}>
          Уже есть аккаунт?{" "}
          <Link href="/login" className={styles.link}>Войти</Link>
        </div>
      </div>
    </div>
  );
}