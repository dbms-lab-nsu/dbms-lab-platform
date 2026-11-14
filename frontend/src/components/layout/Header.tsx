"use client";

import { useState } from 'react';
import Link from 'next/link';
import Navigation from './Navigation';
import MobileMenu from './MobileMenu';
import Container from '../ui/Container';
import styles from './Header.module.css';

export default function Header() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

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
            <button 
              className={styles.demoToggle} 
              onClick={() => setIsAuthenticated(!isAuthenticated)}
              title="Переключить состояние авторизации"
            >
              {isAuthenticated ? 'Выйти' : 'Войти'}
            </button>

            {isAuthenticated ? (
              <Link href="/profile" className={styles.profileLink}>
                Личный кабинет
              </Link>
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