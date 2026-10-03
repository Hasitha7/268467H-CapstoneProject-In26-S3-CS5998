export default function App() {
  return (
    <main style={{ fontFamily: 'Arial, sans-serif', padding: '2rem', maxWidth: '960px', margin: '0 auto' }}>
      <h1>Resume Matcher</h1>
      <p>Capstone project frontend scaffold.</p>
      <section style={{ display: 'grid', gap: '1rem', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
        <div style={{ background: '#f3f4f6', padding: '1rem', borderRadius: '12px' }}>
          <h2>Upload</h2>
          <p>Resume and job description upload flow.</p>
        </div>
        <div style={{ background: '#eef2ff', padding: '1rem', borderRadius: '12px' }}>
          <h2>Match</h2>
          <p>Semantic similarity and skill analysis.</p>
        </div>
        <div style={{ background: '#ecfeff', padding: '1rem', borderRadius: '12px' }}>
          <h2>Report</h2>
          <p>Ranking, insights, and evaluation results.</p>
        </div>
      </section>
    </main>
  );
}
