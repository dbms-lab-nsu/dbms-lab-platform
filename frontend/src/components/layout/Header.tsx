"use client";

import Link from "next/link";
import Navigation from "./Navigation";
import MobileMenu from "./MobileMenu";
import Container from "../ui/Container";
import styles from "./Header.module.css";
import { useAuth } from "@/context/AuthContext";

export default function Header() {
  const { status, user, logout } = useAuth();

  return (
    <header className={styles.header}>
      <Container>
        <div className={styles.headerInner}>
          <div className={styles.logoArea}>
            <Link href="/" className={styles.logo}>
              DBMS Lab
            </Link>
          </div>

          <div className={styles.desktopNavArea}>
            <Navigation />
          </div>

          <div className={styles.authArea}>
            {status === "loading" ? (
              <span className={styles.authSkeleton} aria-hidden />
            ) : status === "authenticated" && user ? (
              <>
                <span className={styles.userName} title={user.email}>
                  {user.firstName} {user.lastName}
                </span>
                <Link href="/profile" className={styles.profileLink}>
                  Личный кабинет
                </Link>
                <button className={styles.logoutBtn} onClick={logout} type="button">
                  Выйти
                </button>
              </>
            ) : (
              <div className={styles.authLinks}>
                <Link href="/login" className={styles.loginLink}>Вход</Link>
                <Link href="/register" className={styles.registerBtn}>Регистрация</Link>
              </div>
            )}
          </div>

          <MobileMenu />
        </div>
      </Container>
    </header>
  );
}