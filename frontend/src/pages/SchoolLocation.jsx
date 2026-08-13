const SCHOOL_QUERY = 'PWH7+J75 Azhar Al Bekaa, Al Azhar Road, Azhar, Lebanon'
const MAP_URL = `https://www.google.com/maps?q=${encodeURIComponent(SCHOOL_QUERY)}&output=embed`
const DIRECTIONS_URL = `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(SCHOOL_QUERY)}&travelmode=driving`

export default function SchoolLocation() {
  return (
    <section className="location-page">
      <div className="page-heading location-heading">
        <div>
          <p className="eyebrow">Visit us</p>
          <h1>School location</h1>
          <p>Find Azhar Al Bekaa on the map or get directions from your current location.</p>
        </div>
        <a className="button directions-button" href={DIRECTIONS_URL} target="_blank" rel="noreferrer">
          Get directions <span aria-hidden="true">↗</span>
        </a>
      </div>

      <div className="location-card">
        <iframe
          className="school-map"
          src={MAP_URL}
          title="Azhar Al Bekaa school location on Google Maps"
          loading="lazy"
          referrerPolicy="no-referrer-when-downgrade"
          allowFullScreen
        />
        <div className="location-details">
          <div className="location-pin" aria-hidden="true">●</div>
          <div>
            <h2>Azhar Al Bekaa</h2>
            <p>PWH7+J75, Al Azhar Road, Azhar, Lebanon</p>
          </div>
          <a href={DIRECTIONS_URL} target="_blank" rel="noreferrer">Open in Google Maps</a>
        </div>
      </div>
    </section>
  )
}
