"use client";

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import styles from './MobileMenu.module.css';

const navItems = [
  { label: 'Главная', href: '/' },
  { label: 'Каталог курсов', href: '/courses' },
  { label: 'Вход', href: '/login' },
  { label: 'Регистрация', href: '/register' },
  { label: 'Личный кабинет', href: '/profile' },
];

export default function MobileMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const pathname = usePathname();

  const toggleMenu = () => setIsOpen(!isOpen);

  return (
    <div className={styles.mobileWrapper}>
      <button 
        className={styles.burger} 
        onClick={toggleMenu}
        aria-label="Toggle menu"
      >
        <span className={`${styles.burgerLine} ${isOpen ? styles.open : ''}`}></span>
        <span className={`${styles.burgerLine} ${isOpen ? styles.open : ''}`}></span>
        <span className={`${styles.burgerLine} ${isOpen ? styles.open : ''}`}></span>
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
                    className={`${styles.mobileNavLink} ${isActive ? styles.active : ''}`}
                    onClick={() => setIsOpen(false)}
                  >
                    {item.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
}