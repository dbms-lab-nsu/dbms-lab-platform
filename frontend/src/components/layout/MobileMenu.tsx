"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import styles from "./MobileMenu.module.css";
import { useAuth } from "@/context/AuthContext";

const publicNavItems = [
  { label: "Главная", href: "/" },
  { label: "Каталог курсов", href: "/courses" },
];

const guestNavItems = [
  { label: "Вход", href: "/login" },
  { label: "Регистрация", href: "/register" },
];

const authNavItems = [
  { label: "Личный кабинет", href: "/profile" },
];

export default function MobileMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const pathname = usePathname();
  const { status, logout, refreshMe } = useAuth();

  const toggleMenu = () => setIsOpen(!isOpen);

  const authItems = status === "authenticated" ? authNavItems : guestNavItems;
  const navItems = [
    ...publicNavItems,
    ...(status === "loading" || status === "error" ? [] : authItems),
  ];

  return (
    <div className={styles.mobileWrapper}>
      <button
        className={styles.burger}
        onClick={toggleMenu}
        aria-label="Toggle menu"
      >
        <span className={`${styles.burgerLine} ${isOpen ? styles.open : ""}`}></span>
        <span className={`${styles.burgerLine} ${isOpen ? styles.open : ""}`}></span>
        <span className={`${styles.burgerLine} ${isOpen ? styles.open : ""}`}></span>
      </button>

      {isOpen && (
        <div className={styles.mobileMenu}>
          <ul className={styles.mobileNavList}>
            {navItems.map((item) => {
              const isActive = pathname === item.href;
              return (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    className={`${styles.mobileNavLink} ${isActive ? styles.active : ""}`}
                    onClick={() => setIsOpen(false)}
                  >
                    {item.label}
                  </Link>
                </li>
              );
            })}
            {status === "error" && (
              <li>
                <button
                  type="button"
                  className={`${styles.mobileNavLink} ${styles.logoutButton}`}
                  onClick={() => {
                    refreshMe();
                    setIsOpen(false);
                  }}
                >
                  Повторить
                </button>
              </li>
            )}
            {status === "authenticated" && (
              <li>
                <button
                  type="button"
                  className={`${styles.mobileNavLink} ${styles.logoutButton}`}
                  onClick={() => {
                    logout();
                    setIsOpen(false);
                  }}
                >
                  Выйти
                </button>
              </li>
            )}
          </ul>
        </div>
      )}
    </div>
  );
}