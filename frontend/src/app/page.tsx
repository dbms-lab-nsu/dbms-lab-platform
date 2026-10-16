import Image from "next/image";
import styles from "./page.module.css";

export default function Home() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

  return (
    <main style={{ padding: "48px", fontFamily: "Arial, sans-serif" }}>
      <h1>DBMS Lab</h1>

      <p>
        Образовательная платформа для изучения внутреннего устройства PostgreSQL.
      </p>

      <section style={{ marginTop: "32px" }}>
        <h2>Пред-MVP</h2>

        <ul>
          <li>Frontend запущен</li>
          <li>Backend API: {apiUrl}</li>
          <li>PostgreSQL будет подключён через Docker Compose</li>
        </ul>
      </section>
    </main>
  );
}