-- =============================================================
--  Tour Planner – PostgreSQL Initialization Script
--  Compatible with: PostgreSQL 14+
-- =============================================================

-- Enable extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================
--  Drop existing tables (for a clean reset)
-- =============================================================
DROP TABLE IF EXISTS tour_log CASCADE;
DROP TABLE IF EXISTS tour     CASCADE;
DROP TABLE IF EXISTS app_user CASCADE;
DROP TYPE  IF EXISTS transport_type CASCADE;

-- =============================================================
--  ENUM: Transport types
-- =============================================================
CREATE TYPE transport_type AS ENUM (
    'CAR',
    'BIKE',
    'WALKING'
);

-- =============================================================
--  TABLE: app_user
--  Note: "user" is a reserved keyword in PostgreSQL,
--        therefore "app_user" is used as the table name.
-- =============================================================
CREATE TABLE app_user (
                          id            UUID         NOT NULL DEFAULT gen_random_uuid(),
                          username      VARCHAR(50)  NOT NULL,
                          email         VARCHAR(255) NOT NULL,
                          password_hash VARCHAR(255) NOT NULL,
                          created_at    TIMESTAMP    NOT NULL DEFAULT NOW(),

                          CONSTRAINT pk_app_user       PRIMARY KEY (id),
                          CONSTRAINT uq_user_username  UNIQUE (username),
                          CONSTRAINT uq_user_email     UNIQUE (email),
                          CONSTRAINT ck_user_email_fmt CHECK (email LIKE '%@%')
);

COMMENT ON TABLE  app_user               IS 'Registered users of the application';
COMMENT ON COLUMN app_user.password_hash IS 'BCrypt hash of the password – never store plaintext';

-- =============================================================
--  TABLE: tour
-- =============================================================
CREATE TABLE tour (
                      id                  UUID           NOT NULL DEFAULT gen_random_uuid(),
                      user_id             UUID           NOT NULL,
                      name                VARCHAR(200)   NOT NULL,
                      description         TEXT,
                      from_location       VARCHAR(300)   NOT NULL,
                      to_location         VARCHAR(300)   NOT NULL,
                      transport_type      transport_type NOT NULL,
                      distance_km         NUMERIC(10,2),               -- populated via OpenRouteService API
                      estimated_time_min  INTEGER,                     -- populated via OpenRouteService API
                      map_image_path      VARCHAR(500),                -- path on filesystem (not a BLOB)
                      popularity          NUMERIC(5,2)   NOT NULL DEFAULT 0.0,   -- derived from number of logs
                      child_friendliness  NUMERIC(5,2)   NOT NULL DEFAULT 0.0,   -- derived from log statistics
                      created_at          TIMESTAMP      NOT NULL DEFAULT NOW(),
                      updated_at          TIMESTAMP      NOT NULL DEFAULT NOW(),

                      CONSTRAINT pk_tour            PRIMARY KEY (id),
                      CONSTRAINT fk_tour_user       FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE,
                      CONSTRAINT ck_tour_distance   CHECK (distance_km IS NULL OR distance_km >= 0),
                      CONSTRAINT ck_tour_time       CHECK (estimated_time_min IS NULL OR estimated_time_min >= 0),
                      CONSTRAINT ck_tour_popularity CHECK (popularity >= 0),
                      CONSTRAINT ck_tour_child_fr   CHECK (child_friendliness >= 0 AND child_friendliness <= 10)
);

COMMENT ON TABLE  tour                    IS 'Tours created by a user';
COMMENT ON COLUMN tour.map_image_path     IS 'Relative path to the map image within the configured base directory';
COMMENT ON COLUMN tour.popularity         IS 'Auto-computed: derived from the total number of tour logs';
COMMENT ON COLUMN tour.child_friendliness IS 'Auto-computed from difficulty, time and distance values of logs (0-10)';

-- Index for fast lookup of all tours belonging to a user
CREATE INDEX idx_tour_user_id ON tour(user_id);

-- Full-text search index over name, description and locations
CREATE INDEX idx_tour_fulltext ON tour
    USING GIN (to_tsvector('english', coalesce(name,'') || ' ' || coalesce(description,'') || ' ' || from_location || ' ' || to_location));

-- =============================================================
--  TABLE: tour_log
-- =============================================================
CREATE TABLE tour_log (
                          id                  UUID          NOT NULL DEFAULT gen_random_uuid(),
                          tour_id             UUID          NOT NULL,
                          date_time           TIMESTAMP     NOT NULL,
                          comment             TEXT,
                          difficulty          SMALLINT      NOT NULL,
                          total_distance_km   NUMERIC(10,2) NOT NULL,
                          total_time_min      INTEGER       NOT NULL,
                          rating              SMALLINT      NOT NULL,

                          CONSTRAINT pk_tour_log       PRIMARY KEY (id),
                          CONSTRAINT fk_tour_log_tour  FOREIGN KEY (tour_id) REFERENCES tour(id) ON DELETE CASCADE,
                          CONSTRAINT ck_log_difficulty CHECK (difficulty BETWEEN 1 AND 5),
                          CONSTRAINT ck_log_rating     CHECK (rating BETWEEN 1 AND 5),
                          CONSTRAINT ck_log_distance   CHECK (total_distance_km >= 0),
                          CONSTRAINT ck_log_time       CHECK (total_time_min >= 0)
);

COMMENT ON TABLE  tour_log            IS 'Recorded statistics of a completed tour';
COMMENT ON COLUMN tour_log.difficulty IS '1 = very easy, 5 = very hard';
COMMENT ON COLUMN tour_log.rating     IS '1 = poor, 5 = excellent';

-- Index for fast log lookups per tour
CREATE INDEX idx_tour_log_tour_id ON tour_log(tour_id);

-- Full-text search index on log comments
CREATE INDEX idx_tour_log_fulltext ON tour_log
    USING GIN (to_tsvector('english', coalesce(comment,'')));

-- =============================================================
--  TRIGGER: automatically update updated_at on tour
-- =============================================================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_tour_updated_at
    BEFORE UPDATE ON tour
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- =============================================================
--  VIEW: Full-text search (combines tour + logs)
-- =============================================================
CREATE OR REPLACE VIEW v_tour_search AS
SELECT
    t.id                                              AS tour_id,
    t.user_id,
    t.name,
    t.description,
    t.from_location,
    t.to_location,
    t.transport_type,
    t.distance_km,
    t.estimated_time_min,
    t.popularity,
    t.child_friendliness,
    COUNT(l.id)                                       AS log_count,
    AVG(l.rating)                                     AS avg_rating,
    AVG(l.difficulty)                                 AS avg_difficulty,
    to_tsvector('english',
                coalesce(t.name,'')        || ' ' ||
                coalesce(t.description,'') || ' ' ||
                t.from_location            || ' ' ||
                t.to_location              || ' ' ||
                coalesce(string_agg(l.comment, ' '),'')
    )                                                 AS search_vector
FROM tour t
         LEFT JOIN tour_log l ON l.tour_id = t.id
GROUP BY t.id;

COMMENT ON VIEW v_tour_search IS 'Combined full-text search index across tours and all related logs';

-- =============================================================
--  EXAMPLE FULL-TEXT QUERY (for testing in DataGrip):
--
--  SELECT tour_id, name, from_location, to_location
--  FROM   v_tour_search
--  WHERE  user_id = '<your-user-uuid>'
--    AND  search_vector @@ plainto_tsquery('english', 'Vienna bike');
-- =============================================================

-- =============================================================
--  SAMPLE DATA (optional – keep commented out for production)
-- =============================================================
/*
INSERT INTO app_user (username, email, password_hash)
VALUES ('testuser', 'test@example.com', '$2a$12$PLACEHOLDER_BCRYPT_HASH');

INSERT INTO tour (user_id, name, description, from_location, to_location, transport_type, distance_km, estimated_time_min)
SELECT id, 'Danube Cycle Path', 'Scenic bike tour along the Danube river', 'Vienna', 'Krems', 'BIKE', 73.5, 240
FROM app_user WHERE username = 'testuser';

INSERT INTO tour_log (tour_id, date_time, comment, difficulty, total_distance_km, total_time_min, rating)
SELECT t.id, NOW(), 'Great ride, light headwind', 2, 73.5, 255, 5
FROM tour t
JOIN app_user u ON t.user_id = u.id
WHERE u.username = 'testuser';
*/