"use client";

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import styles from './Navigation.module.css';

const navItems = [
  { label: 'Главная', href: '/' },
  { label: 'Каталог курсов', href: '/courses' },
];

export default function Navigation() {
  const pathname = usePathname();

  return (
    <nav className={styles.desktopNav}>
      <ul className={styles.navList}>
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          return (
            <li key={item.href}>
              <Link 
                href={item.href} 
                className={`${styles.navLink} ${isActive ? styles.active : ''}`}
              >
                {item.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}