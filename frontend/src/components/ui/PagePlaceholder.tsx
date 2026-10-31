import Container from './Container';

interface PagePlaceholderProps {
  title: string;
  description: string;
}

export default function PagePlaceholder({ title, description }: PagePlaceholderProps) {
  return (
    <Container>
      <div style={{ padding: '64px 0', textAlign: 'center' }}>
        <h1 style={{ fontSize: '32px', marginBottom: '16px' }}>{title}</h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '18px', maxWidth: '600px', margin: '0 auto' }}>
          {description}
        </p>
        <div style={{ marginTop: '32px', padding: '16px', border: '1px dashed var(--border-color)', borderRadius: '8px', display: 'inline-block' }}>
          Страница в разработке
        </div>
      </div>
    </Container>
  );
}