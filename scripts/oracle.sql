-- Create a new database user to avoid using sys user
CREATE USER music IDENTIFIED BY password;

-- Grant all privileges to the new user
GRANT ALL PRIVILEGES TO music;

-- Connect to the new user
CONNECT music/music;


CREATE OR REPLACE AND COMPILE
    JAVA SOURCE NAMED "RandomUUID" AS
    import java.util.UUID;
    public class RandomUUID {
        public static String generateUUID() {
            return UUID.randomUUID().toString();
        }
    };
/

CREATE OR REPLACE FUNCTION gen_random_uuid
    RETURN VARCHAR2
    AS LANGUAGE JAVA
    NAME 'RandomUUID.generateUUID() return String';
/

SELECT gen_random_uuid() FROM dual;

CREATE TABLE IF NOT EXISTS users (
    user_id VARCHAR2(36) NOT NULL UNIQUE,
    username VARCHAR2(100) NOT NULL,
    password VARCHAR2(100) NOT NULL,
    gender VARCHAR2(6) NOT NULL CHECK(gender IN ('male', 'female')),
    country VARCHAR2(100) NOT NULL,
    user_image BLOB NOT NULL,
    playlist_count NUMBER(10) DEFAULT 0 NOT NULL,
    favorite_songs_count NUMBER(10) DEFAULT 0 NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (user_id, username)
);

CREATE OR REPLACE TRIGGER users_before_insert
    BEFORE INSERT ON users
    FOR EACH ROW
BEGIN
    IF :new.user_id IS NULL THEN
        :new.user_id := gen_random_uuid();
    END IF;
END;
/

-- Insert into play state table when user is created
CREATE OR REPLACE TRIGGER users_after_insert
    AFTER INSERT ON users
    FOR EACH ROW
BEGIN
    INSERT INTO play_state (user_id, tracks_queue, current_track, loop, shuffle) VALUES (:new.user_id, queue_type(), NULL, 0, 0);
END;
/

CREATE TABLE IF NOT EXISTS artists (
    artist_id VARCHAR2(36) NOT NULL UNIQUE,
    user_id VARCHAR2(36) NOT NULL,
    artist_name VARCHAR2(100) NOT NULL,
    artist_bio VARCHAR2(1000) NOT NULL,
    artist_image BLOB NOT NULL,
    track_count NUMBER(10) DEFAULT 0 NOT NULL,
    album_count NUMBER(10) DEFAULT 0 NOT NULL,
    follower_count NUMBER(10) DEFAULT 0 NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (artist_id, artist_name),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER artists_before_insert
    BEFORE INSERT ON artists
    FOR EACH ROW
BEGIN
    IF :new.artist_id IS NULL THEN
        :new.artist_id := gen_random_uuid();
    END IF;
END;
/

CREATE TABLE IF NOT EXISTS albums (
    album_id VARCHAR2(36) NOT NULL UNIQUE,
    album_name VARCHAR2(100) NOT NULL,
    album_image BLOB NOT NULL,
    release_date DATE NOT NULL,
    track_count NUMBER(10) DEFAULT 0 NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (album_id, album_name)
);

CREATE OR REPLACE TRIGGER albums_before_insert
    BEFORE INSERT ON albums
    FOR EACH ROW
BEGIN
    IF :new.album_id IS NULL THEN
        :new.album_id := gen_random_uuid();
    END IF;
END;
/

CREATE TABLE IF NOT EXISTS tracks (
    track_id VARCHAR2(36) NOT NULL UNIQUE,
    track_name VARCHAR2(100) NOT NULL,
    track_audio BLOB NOT NULL,
    track_duration INTERVAL DAY TO SECOND NOT NULL,
    track_language VARCHAR2(100) NOT NULL,
    track_album_id VARCHAR2(36),
    lyrics VARCHAR2(1000) NOT NULL,
    release_date DATE NOT NULL,
    streams NUMBER(10) DEFAULT 0 NOT NULL,
    like_count NUMBER(10) DEFAULT 0 NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (track_id, track_name),
    FOREIGN KEY (track_album_id) REFERENCES albums(album_id) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER tracks_before_insert
    BEFORE INSERT ON tracks
    FOR EACH ROW
BEGIN
    IF :new.track_id IS NULL THEN
        :new.track_id := gen_random_uuid();
    END IF;
END;
/

CREATE TABLE IF NOT EXISTS genres (
    genre_id VARCHAR2(36) NOT NULL UNIQUE,
    genre_name VARCHAR2(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (genre_id, genre_name)
);

-- Drop duplicate genres which have same genre_name, leaving only one
DELETE FROM genres WHERE ROWID NOT IN (SELECT MIN(ROWID) FROM genres GROUP BY genre_name);

-- list total count of duplicate genres
SELECT COUNT(*) FROM (SELECT genre_name FROM genres GROUP BY genre_name HAVING COUNT(*) > 1);

CREATE OR REPLACE TRIGGER genres_before_insert
    BEFORE INSERT ON genres
    FOR EACH ROW
BEGIN
    IF :new.genre_id IS NULL THEN
        :new.genre_id := gen_random_uuid();
    END IF;
END;
/

CREATE TABLE IF NOT EXISTS playlists (
    playlist_id VARCHAR2(36) NOT NULL UNIQUE,
    playlist_name VARCHAR2(100) NOT NULL,
    playlist_image BLOB NOT NULL,
    playlist_description VARCHAR2(1000) NOT NULL CHECK (LENGTH(playlist_description) <= 1000),
    playlist_owner VARCHAR2(36) NOT NULL,
    track_count NUMBER(10) DEFAULT 0 NOT NULL,
    follower_count NUMBER(10) DEFAULT 0 NOT NULL,
    likes_count NUMBER(10) DEFAULT 0 NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (playlist_id, playlist_name),
    FOREIGN KEY (playlist_owner) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER playlists_before_insert
    BEFORE INSERT ON playlists
    FOR EACH ROW
BEGIN
    IF :new.playlist_id IS NULL THEN
        :new.playlist_id := gen_random_uuid();
    END IF;
END;
/

-- creating an array type for queue
CREATE OR REPLACE TYPE queue_type IS TABLE OF VARCHAR2(36) NOT NULL;/

CREATE TABLE IF NOT EXISTS play_state (
    user_id VARCHAR2(36) NOT NULL,
    tracks_queue queue_type,
    current_track VARCHAR2(36) NOT NULL,
    loop BOOLEAN NOT NULL CHECK (loop IN (1, 0)),
    shuffle BOOLEAN NOT NULL CHECK (shuffle IN (1, 0)),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (user_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
) NESTED TABLE tracks_queue STORE AS tracks_queue_table;

CREATE TABLE IF NOT EXISTS user_history (
    user_id VARCHAR2(36) NOT NULL,
    track_id VARCHAR2(36) NOT NULL,
    played_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (user_id, track_id, played_at),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER play_state_after_insert
    AFTER INSERT ON play_state
    FOR EACH ROW
BEGIN
    IF :new.current_track IS NOT NULL THEN
        INSERT INTO user_history (user_id, track_id) VALUES (:new.user_id, :new.current_track);
    END IF;
END;
/

CREATE TABLE IF NOT EXISTS user_activity (
    user_id VARCHAR2(36) NOT NULL,
    activity VARCHAR2(100) NOT NULL CHECK (activity IN ('follow_artist', 'like_track', 'like_playlist', 'follow_playlist')),
    activity_time TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    target_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (user_id, activity, activity_time, target_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

-- TODO: make triggers
CREATE TABLE IF NOT EXISTS analytics (
    today_date DATE DEFAULT CURRENT_DATE NOT NULL,
    user_count NUMBER(10) DEFAULT 0 NOT NULL,
    track_count NUMBER(10) DEFAULT 0 NOT NULL,
    album_count NUMBER(10) DEFAULT 0 NOT NULL,
    artist_count NUMBER(10) DEFAULT 0 NOT NULL,
    playlist_count NUMBER(10) DEFAULT 0 NOT NULL,
    genre_count NUMBER(10) DEFAULT 0 NOT NULL,
    PRIMARY KEY (today_date)
);

DECLARE
    v_user_count NUMBER(10);
    v_track_count NUMBER(10);
    v_album_count NUMBER(10);
    v_artist_count NUMBER(10);
    v_playlist_count NUMBER(10);
    v_genre_count NUMBER(10);
BEGIN
    SELECT COUNT(*) INTO v_user_count FROM users;
    SELECT COUNT(*) INTO v_track_count FROM tracks;
    SELECT COUNT(*) INTO v_album_count FROM albums;
    SELECT COUNT(*) INTO v_artist_count FROM artists;
    SELECT COUNT(*) INTO v_playlist_count FROM playlists;
    SELECT COUNT(*) INTO v_genre_count FROM genres;
    INSERT INTO analytics (today_date, user_count, track_count, album_count, artist_count, playlist_count, genre_count) VALUES (CURRENT_DATE, v_user_count, v_track_count, v_album_count, v_artist_count, v_playlist_count, v_genre_count);
END;
/

-- trigger for insert into users
CREATE OR REPLACE TRIGGER users_after_insert
    AFTER INSERT ON users
    FOR EACH ROW
BEGIN
    UPDATE analytics SET user_count = user_count + 1 WHERE today_date = CURRENT_DATE;
END;
/

-- trigger for insert into tracks
CREATE OR REPLACE TRIGGER tracks_after_insert
    AFTER INSERT ON tracks
    FOR EACH ROW
BEGIN
    UPDATE analytics SET track_count = track_count + 1 WHERE today_date = CURRENT_DATE;
END;
/

-- trigger for insert into albums
CREATE OR REPLACE TRIGGER albums_after_insert
    AFTER INSERT ON albums
    FOR EACH ROW
BEGIN
    UPDATE analytics SET album_count = album_count + 1 WHERE today_date = CURRENT_DATE;
END;
/

-- trigger for insert into artists
CREATE OR REPLACE TRIGGER artists_after_insert
    AFTER INSERT ON artists
    FOR EACH ROW
BEGIN
    UPDATE analytics SET artist_count = artist_count + 1 WHERE today_date = CURRENT_DATE;
END;
/

-- trigger for insert into playlists
CREATE OR REPLACE TRIGGER playlists_after_insert
    AFTER INSERT ON playlists
    FOR EACH ROW
BEGIN
    UPDATE analytics SET playlist_count = playlist_count + 1 WHERE today_date = CURRENT_DATE;
END;
/

CREATE TABLE IF NOT EXISTS user_playlists (
    user_id VARCHAR2(36) NOT NULL,
    playlist_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (user_id, playlist_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    FOREIGN KEY (playlist_id) REFERENCES playlists(playlist_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS user_favorite_songs (
    user_id VARCHAR2(36) NOT NULL,
    track_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (user_id, track_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS user_saved_albums (
    user_id VARCHAR2(36) NOT NULL,
    album_id VARCHAR2(36) NOT NULL,
    saved_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (user_id, album_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    FOREIGN KEY (album_id) REFERENCES albums(album_id) ON DELETE CASCADE
);

ALTER TABLE users ADD saved_albums_count NUMBER(10) DEFAULT 0 NOT NULL;

CREATE OR REPLACE TRIGGER user_saved_albums_after_insert
    AFTER INSERT ON user_saved_albums
    FOR EACH ROW
BEGIN
    UPDATE users SET saved_albums_count = saved_albums_count + 1 WHERE user_id = :new.user_id;
END;
/

CREATE OR REPLACE TRIGGER user_saved_albums_after_delete
    AFTER DELETE ON user_saved_albums
    FOR EACH ROW
BEGIN
    UPDATE users SET saved_albums_count = saved_albums_count - 1 WHERE user_id = :old.user_id;
END;
/


CREATE TABLE IF NOT EXISTS artist_tracks (
    artist_id VARCHAR2(36) NOT NULL,
    track_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (artist_id, track_id),
    FOREIGN KEY (artist_id) REFERENCES artists(artist_id) ON DELETE CASCADE,
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS artist_albums (
    artist_id VARCHAR2(36) NOT NULL,
    album_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (artist_id, album_id),
    FOREIGN KEY (artist_id) REFERENCES artists(artist_id) ON DELETE CASCADE,
    FOREIGN KEY (album_id) REFERENCES albums(album_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS artist_genres (
    artist_id VARCHAR2(36) NOT NULL,
    genre_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (artist_id, genre_id),
    FOREIGN KEY (artist_id) REFERENCES artists(artist_id) ON DELETE CASCADE,
    FOREIGN KEY (genre_id) REFERENCES genres(genre_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS artist_followers (
    artist_id VARCHAR2(36) NOT NULL,
    user_id VARCHAR2(36) NOT NULL,
    followed_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (artist_id, user_id),
    FOREIGN KEY (artist_id) REFERENCES artists(artist_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER artist_followers_after_insert
    AFTER INSERT ON artist_followers
    FOR EACH ROW
BEGIN
    UPDATE artists SET follower_count = follower_count + 1 WHERE artist_id = :new.artist_id;
END;
/

CREATE OR REPLACE TRIGGER artist_followers_after_delete
    AFTER DELETE ON artist_followers
    FOR EACH ROW
BEGIN
    UPDATE artists SET follower_count = follower_count - 1 WHERE artist_id = :old.artist_id;
END;
/

CREATE TABLE IF NOT EXISTS album_artists (
    album_id VARCHAR2(36) NOT NULL,
    artist_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (album_id, artist_id),
    FOREIGN KEY (album_id) REFERENCES albums(album_id) ON DELETE CASCADE,
    FOREIGN KEY (artist_id) REFERENCES artists(artist_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS album_genres (
    album_id VARCHAR2(36) NOT NULL,
    genre_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (album_id, genre_id),
    FOREIGN KEY (album_id) REFERENCES albums(album_id) ON DELETE CASCADE,
    FOREIGN KEY (genre_id) REFERENCES genres(genre_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS album_tracks (
    album_id VARCHAR2(36) NOT NULL,
    track_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (album_id, track_id),
    FOREIGN KEY (album_id) REFERENCES albums(album_id) ON DELETE CASCADE,
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS track_artists (
    track_id VARCHAR2(36) NOT NULL,
    artist_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (track_id, artist_id),
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE,
    FOREIGN KEY (artist_id) REFERENCES artists(artist_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS track_genres (
    track_id VARCHAR2(36) NOT NULL,
    genre_id VARCHAR2(36) NOT NULL,
    PRIMARY KEY (track_id, genre_id),
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE,
    FOREIGN KEY (genre_id) REFERENCES genres(genre_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS track_likes (
    track_id VARCHAR2(36) NOT NULL,
    user_id VARCHAR2(36) NOT NULL,
    liked_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (track_id, user_id),
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER track_likes_after_insert
    AFTER INSERT ON track_likes
    FOR EACH ROW
BEGIN
    UPDATE tracks SET like_count = like_count + 1 WHERE track_id = :new.track_id;
    UPDATE users SET favorite_songs_count = favorite_songs_count + 1 WHERE user_id = :new.user_id;
    INSERT INTO user_favorite_songs (user_id, track_id) VALUES (:new.user_id, :new.track_id);
END;
/

CREATE OR REPLACE TRIGGER track_likes_after_delete
    AFTER DELETE ON track_likes
    FOR EACH ROW
BEGIN
    UPDATE tracks SET like_count = like_count - 1 WHERE track_id = :old.track_id;
    UPDATE users SET favorite_songs_count = favorite_songs_count - 1 WHERE user_id = :old.user_id;
    DELETE FROM user_favorite_songs WHERE user_id = :old.user_id AND track_id = :old.track_id;
END;
/

CREATE TABLE IF NOT EXISTS playlist_followers (
    playlist_id VARCHAR2(36) NOT NULL,
    user_id VARCHAR2(36) NOT NULL,
    followed_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (playlist_id, user_id),
    FOREIGN KEY (playlist_id) REFERENCES playlists(playlist_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS playlist_likes (
    playlist_id VARCHAR2(36) NOT NULL,
    user_id VARCHAR2(36) NOT NULL,
    liked_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (playlist_id, user_id),
    FOREIGN KEY (playlist_id) REFERENCES playlists(playlist_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE OR REPLACE TRIGGER playlist_likes_after_insert
    AFTER INSERT ON playlist_likes
    FOR EACH ROW
BEGIN
    UPDATE playlists SET likes_count = likes_count + 1 WHERE playlist_id = :new.playlist_id;
END;
/

CREATE OR REPLACE TRIGGER playlist_likes_after_delete
    AFTER DELETE ON playlist_likes
    FOR EACH ROW
BEGIN
    UPDATE playlists SET likes_count = likes_count - 1 WHERE playlist_id = :old.playlist_id;
END;
/

CREATE OR REPLACE TRIGGER playlist_followers_after_insert
    AFTER INSERT ON playlist_followers
    FOR EACH ROW
BEGIN
    UPDATE playlists SET follower_count = follower_count + 1 WHERE playlist_id = :new.playlist_id;
    INSERT INTO user_playlists (user_id, playlist_id) VALUES (:new.user_id, :new.playlist_id);
END;
/

CREATE OR REPLACE TRIGGER playlist_followers_after_delete
    AFTER DELETE ON playlist_followers
    FOR EACH ROW
BEGIN
    UPDATE playlists SET follower_count = follower_count - 1 WHERE playlist_id = :old.playlist_id;
    DELETE FROM user_playlists WHERE user_id = :old.user_id AND playlist_id = :old.playlist_id;
END;
/

CREATE TABLE IF NOT EXISTS playlist_tracks (
    playlist_id VARCHAR2(36) NOT NULL,
    track_id VARCHAR2(36) NOT NULL,
    added_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (playlist_id, track_id),
    FOREIGN KEY (playlist_id) REFERENCES playlists(playlist_id) ON DELETE CASCADE,
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE
);

DROP TRIGGER users_before_insert;
DROP TRIGGER artists_before_insert;
DROP TRIGGER albums_before_insert;
DROP TRIGGER tracks_before_insert;
DROP TRIGGER genres_before_insert;
DROP TRIGGER playlists_before_insert;

DROP FUNCTION gen_random_uuid;
SELECT sys_context('userenv', 'current_schema') FROM dual;  -- Prints schema
DROP JAVA SOURCE "SYS"."RandomUUID";

DROP TABLE users CASCADE CONSTRAINTS;
DROP TABLE artists CASCADE CONSTRAINTS;
DROP TABLE albums CASCADE CONSTRAINTS;
DROP TABLE tracks CASCADE CONSTRAINTS;
DROP TABLE genres CASCADE CONSTRAINTS;
DROP TABLE playlists CASCADE CONSTRAINTS;
DROP TABLE play_state CASCADE CONSTRAINTS;
DROP TABLE user_history CASCADE CONSTRAINTS;
DROP TABLE user_activity CASCADE CONSTRAINTS;
DROP TABLE analytics CASCADE CONSTRAINTS;
DROP TABLE user_playlists CASCADE CONSTRAINTS;
DROP TABLE user_favorite_songs CASCADE CONSTRAINTS;
DROP TABLE artist_tracks CASCADE CONSTRAINTS;
DROP TABLE artist_albums CASCADE CONSTRAINTS;
DROP TABLE artist_followers CASCADE CONSTRAINTS;
DROP TABLE album_artists CASCADE CONSTRAINTS;
DROP TABLE album_genres CASCADE CONSTRAINTS;
DROP TABLE album_tracks CASCADE CONSTRAINTS;
DROP TABLE track_artists CASCADE CONSTRAINTS;
DROP TABLE track_genres CASCADE CONSTRAINTS;
DROP TABLE track_likes CASCADE CONSTRAINTS;
DROP TABLE playlist_followers CASCADE CONSTRAINTS;
DROP TABLE playlist_likes CASCADE CONSTRAINTS;
DROP TABLE playlist_tracks CASCADE CONSTRAINTS;

DROP USER music CASCADE;

SET WRAP OFF
SET LINESIZE 132
SET PAGESIZE 50000
SET TAB OFF
SET AUTOCOMMIT ON
SET SERVEROUTPUT ON
SET LINES 256
SET TRIMOUT ON
SET ERRORLOGGING ON
SET HISTORY ON


INSERT INTO users (username, password, gender, country, user_image) VALUES ('john_doe', 'test1234', 'male', 'USA', EMPTY_BLOB());
INSERT INTO artists(user_id, artist_name, artist_bio, artist_image) VALUES ('3c935621-aa82-4d48-b97a-928fa26219c0', 'triyan', 'boht bada aadmi', EMPTY_BLOB());
INSERT INTO albums (album_name, album_image, release_date) VALUES ('dev', EMPTY_BLOB(), To_date('11/11/2004', 'dd/mm/yyyy'));
INSERT INTO tracks (track_name, track_audio, track_duration, track_language, track_album_id, lyrics, release_date) VALUES('abcd', EMPTY_BLOB(), INTERVAL '0 00:04:30.123' DAY TO SECOND(3), 'Angrezi', 'ba4b404e-d03b-4777-909e-723106eab260', 'ghfhfgghfhg', To_date('11/11/2001', 'dd/mm/yyyy'));
INSERT INTO genres (genre_name) VALUES ('triyan ke ajoobe');
INSERT INTO playlists (playlist_name, playlist_image, playlist_description, playlist_owner) VALUES ('random', EMPTY_BLOB(), 'dewudbkegd', '3c935621-aa82-4d48-b97a-928fa26219c0');
INSERT INTO play_state (user_id, queue, current_track, loop, shuffle) VALUES ('3c935621-aa82-4d48-b97a-928fa26219c0', queue_type('3cda5e2f-3858-47cd-b2c2-1f86956fe883'), '3cda5e2f-3858-47cd-b2c2-1f86956fe883', 0, 1);
INSERT INTO user_history (user_id, track_id, played_at) VALUES ('3c935621-aa82-4d48-b97a-928fa26219c0', '3cda5e2f-3858-47cd-b2c2-1f86956fe883', CURRENT_TIMESTAMP);
INSERT INTO user_activity (user_id, activity, activity_time, target_id) VALUES ('3c935621-aa82-4d48-b97a-928fa26219c0','follow_artist', CURRENT_TIMESTAMP, 'a63f379f-1633-4a89-8322-7125fe6f6ca2');
INSERT INTO analytics (today_date) VALUES (CURRENT_DATE);
INSERT INTO user_playlists (user_id, playlist_id) VALUES ('3c935621-aa82-4d48-b97a-928fa26219c0','e89f60e6-c835-47c0-af97-3fd10f0802bb' );
INSERT INTO user_favorite_songs (user_id, track_id) VALUES ('3c935621-aa82-4d48-b97a-928fa26219c0', '3cda5e2f-3858-47cd-b2c2-1f86956fe883');
INSERT INTO artist_tracks (artist_id, track_id) VALUES ('a63f379f-1633-4a89-8322-7125fe6f6ca2', '3cda5e2f-3858-47cd-b2c2-1f86956fe883' );
INSERT INTO artist_albums (artist_id, album_id) VALUES ('a63f379f-1633-4a89-8322-7125fe6f6ca2', 'ba4b404e-d03b-4777-909e-723106eab260');
INSERT INTO artist_followers (artist_id, user_id, followed_at) VALUES ('a63f379f-1633-4a89-8322-7125fe6f6ca2', '3c935621-aa82-4d48-b97a-928fa26219c0', CURRENT_TIMESTAMP);
INSERT INTO album_artists (album_id, artist_id)  VALUES ('ba4b404e-d03b-4777-909e-723106eab260', 'a63f379f-1633-4a89-8322-7125fe6f6ca2');
INSERT INTO album_genres (album_id, genre_id) VALUES ('ba4b404e-d03b-4777-909e-723106eab260', '623257f6-e66f-4e41-825d-960ae3eeec94');
INSERT INTO album_tracks (album_id, track_id) VALUES ('ba4b404e-d03b-4777-909e-723106eab260', '3cda5e2f-3858-47cd-b2c2-1f86956fe883');
INSERT INTO track_artists (track_id, artist_id) VALUES ('3cda5e2f-3858-47cd-b2c2-1f86956fe883','a63f379f-1633-4a89-8322-7125fe6f6ca2');
INSERT INTO track_genres (track_id, genre_id) VALUES ('3cda5e2f-3858-47cd-b2c2-1f86956fe883', '623257f6-e66f-4e41-825d-960ae3eeec94');
INSERT INTO track_likes (track_id, user_id, liked_at) VALUES ('3cda5e2f-3858-47cd-b2c2-1f86956fe883', '3c935621-aa82-4d48-b97a-928fa26219c0', CURRENT_TIMESTAMP);
INSERT INTO playlist_followers (playlist_id, user_id, followed_at) VALUES ('e89f60e6-c835-47c0-af97-3fd10f0802bb', '3c935621-aa82-4d48-b97a-928fa26219c0', CURRENT_TIMESTAMP);
INSERT INTO playlist_likes (playlist_id, user_id, liked_at) VALUES ('e89f60e6-c835-47c0-af97-3fd10f0802bb', '3c935621-aa82-4d48-b97a-928fa26219c0', CURRENT_TIMESTAMP);
INSERT INTO playlist_tracks (playlist_id, track_id, added_at) VALUES ('e89f60e6-c835-47c0-af97-3fd10f0802bb', '3cda5e2f-3858-47cd-b2c2-1f86956fe883', CURRENT_TIMESTAMP);

DELETE FROM users;
DELETE FROM artists;
DELETE FROM albums;
DELETE FROM tracks;
DELETE FROM genres;
DELETE FROM playlists;
DELETE FROM play_state;
DELETE FROM user_history;
DELETE FROM user_activity;
DELETE FROM analytics;
DELETE FROM user_playlists;
DELETE FROM user_favorite_songs;
DELETE FROM artist_tracks;
DELETE FROM artist_albums;
DELETE FROM artist_followers;
DELETE FROM album_artists;
DELETE FROM album_genres;
DELETE FROM album_tracks;
DELETE FROM track_artists;
DELETE FROM track_genres;
DELETE FROM track_likes;
DELETE FROM playlist_followers;
DELETE FROM playlist_likes;
DELETE FROM playlist_tracks;

ALTER TABLE tracks DROP COLUMN track_image;
ALTER TABLE users DROP COLUMN language;
ALTER TABLE artists DROP COLUMN artist_image;
ALTER TABLE tracks DROP COLUMN track_language;
ALTER TABLE albums ADD album_type VARCHAR2(100) DEFAULT 'album' NOT NULL CHECK (album_type IN ('album', 'single', 'compilation'));
ALTER TABLE albums ADD album_image_url VARCHAR2(100) NULL;
ALTER TABLE tracks ADD track_audio_url VARCHAR2(100) NULL;
ALTER TABLE artists ADD artist_image_url VARCHAR2(100) NULL;
ALTER TABLE tracks MODIFY lyrics VARCHAR2(2000);
ALTER TABLE tracks DROP COLUMN lyrics;
ALTER TABLE tracks ADD lyrics CLOB NOT NULL;
ALTER TABLE tracks MODIFY track_name VARCHAR2(200);
ALTER TABLE play_state MODIFY current_track VARCHAR2(36) DEFAULT NULL;
ALTER TABLE tradks ADD track_image_url VARCHAR2(100) NULL;
UPDATE tracks SET track_image_url = (SELECT album_image_url FROM albums WHERE tracks.track_album_id = albums.album_id);

ALTER TABLE tracks DROP COLUMN track_audio;
ALTER TABLE tracks DROP COLUMN lyrics;

CREATE TABLE IF NOT EXISTS track_audio (
    track_id VARCHAR2(36),
    track_audio BLOB NOT NULL,
    PRIMARY KEY (track_id),
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS track_lyrics (
    track_id VARCHAR2(36),
    lyrics VARCHAR2(2000) NOT NULL,
    PRIMARY KEY (track_id),
    FOREIGN KEY (track_id) REFERENCES tracks(track_id) ON DELETE CASCADE
);

ALTER TABLE tracks ADD lyrics VARCHAR2(4000) NOT NULL;

-- get lyrics from track_lyrics table and store in tracks table
UPDATE tracks t SET t.lyrics = (SELECT l.lyrics FROM track_lyrics l WHERE t.track_id = l.track_id);

ALTER TABLE tracks_lyrics MODIFY lyrics VARCHAR2(2500);

CREATE INDEX IF NOT EXISTS track_album_id_index ON tracks(track_album_id);

ALTER TABLE tracks ADD audio_available BOOLEAN DEFAULT 0 NOT NULL CHECK (audio_available IN (1, 0));
-- Update audio_available to 1 for tracks where record exists in track_audio table
UPDATE tracks t SET t.audio_available = 1 WHERE EXISTS (SELECT 1 FROM track_audio ta WHERE t.track_id = ta.track_id);

-- show and describe all tables
BEGIN
    FOR r IN (SELECT table_name FROM user_tables) LOOP
        DBMS_OUTPUT.PUT_LINE(r.table_name);
    END LOOP;
END;

-- function to recommend 5 random playlists to a user
CREATE OR REPLACE FUNCTION recommend_playlists(user_id VARCHAR2)
    RETURN SYS_REFCURSOR
    AS
    playlists SYS_REFCURSOR;
    BEGIN
        OPEN playlists FOR
        SELECT playlist_id FROM playlists WHERE playlist_owner != user_id ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY;
        RETURN playlists;
    END;
/

-- function to recommend 5 random tracks to a user
CREATE OR REPLACE FUNCTION recommend_tracks
    RETURN SYS_REFCURSOR
    AS
    tracks SYS_REFCURSOR;
    BEGIN
        OPEN tracks FOR
        SELECT track_id FROM tracks ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY;
        RETURN tracks;
    END;
/

-- function to recommend 5 random artists to a user
CREATE OR REPLACE FUNCTION recommend_artists
    RETURN SYS_REFCURSOR
    AS
    artists SYS_REFCURSOR;
    BEGIN
        OPEN artists FOR
        SELECT artist_id FROM artists ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY;
        RETURN artists;
    END;
/

-- function to recommend 5 random albums to a user
CREATE OR REPLACE FUNCTION recommend_albums
    RETURN SYS_REFCURSOR
    AS
    albums SYS_REFCURSOR;
    BEGIN
        OPEN albums FOR
        SELECT album_id FROM albums ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY;
        RETURN albums;
    END;
/

CREATE OR REPLACE FUNCTION toggle_track_like(user_id VARCHAR2, track_id VARCHAR2)
    RETURN BOOLEAN
    AS
    track_liked BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO track_liked FROM track_likes WHERE user_id = toggle_track_like.user_id AND track_id = toggle_track_like.track_id AND rownum <= 1;
        IF track_liked = FALSE THEN
            INSERT INTO track_likes (track_id, user_id) VALUES (toggle_track_like.track_id, toggle_track_like.user_id);
            RETURN TRUE;
        ELSE
            DELETE FROM track_likes WHERE user_id = toggle_track_like.user_id AND track_id = toggle_track_like.track_id;
            RETURN FALSE;
        END IF;
    END;
/

DECLARE
    v_track_liked BOOLEAN;
BEGIN
    v_track_liked := toggle_track_like('12ab49eb-1689-4d3a-ab8e-ab7d9a51f2c7', '24daa65d-0a78-427c-9533-8d14f7ca9c17');
    IF v_track_liked THEN
        DBMS_OUTPUT.PUT_LINE('Track liked');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Track unliked');
    END IF;
END;
/

CREATE OR REPLACE FUNCTION toggle_artist_follow(user_id VARCHAR2, artist_id VARCHAR2)
    RETURN BOOLEAN
    AS
    artist_followed BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO artist_followed FROM artist_followers WHERE user_id = toggle_artist_follow.user_id AND artist_id = toggle_artist_follow.artist_id AND rownum <= 1;
        IF artist_followed = FALSE THEN
            INSERT INTO artist_followers (artist_id, user_id) VALUES (toggle_artist_follow.artist_id, toggle_artist_follow.user_id);
            RETURN TRUE;
        ELSE
            DELETE FROM artist_followers WHERE user_id = toggle_artist_follow.user_id AND artist_id = toggle_artist_follow.artist_id;
            RETURN FALSE;
        END IF;
    END;
    /

CREATE OR REPLACE FUNCTION toggle_playlist_like(user_id VARCHAR2, playlist_id VARCHAR2)
    RETURN BOOLEAN
    AS
    playlist_liked BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO playlist_liked FROM playlist_likes WHERE user_id = toggle_playlist_like.user_id AND playlist_id = toggle_playlist_like.playlist_id AND rownum <= 1;
        IF playlist_liked = FALSE THEN
            INSERT INTO playlist_likes (playlist_id, user_id) VALUES (toggle_playlist_like.playlist_id, toggle_playlist_like.user_id);
            RETURN TRUE;
        ELSE
            DELETE FROM playlist_likes WHERE user_id = toggle_playlist_like.user_id AND playlist_id = toggle_playlist_like.playlist_id;
            RETURN FALSE;
        END IF;
    END;
    /

CREATE OR REPLACE FUNCTION toggle_playlist_follow(user_id VARCHAR2, playlist_id VARCHAR2)
    RETURN BOOLEAN
    AS
    playlist_followed BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO playlist_followed FROM playlist_followers WHERE user_id = toggle_playlist_follow.user_id AND playlist_id = toggle_playlist_follow.playlist_id AND rownum <= 1;
        IF playlist_followed = FALSE THEN
            INSERT INTO playlist_followers (playlist_id, user_id) VALUES (toggle_playlist_follow.playlist_id, toggle_playlist_follow.user_id);
            RETURN TRUE;
        ELSE
            DELETE FROM playlist_followers WHERE user_id = toggle_playlist_follow.user_id AND playlist_id = toggle_playlist_follow.playlist_id;
            RETURN FALSE;
        END IF;
    END;
    /

CREATE OR REPLACE FUNCTION toggle_album_save(user_id VARCHAR2, album_id VARCHAR2)
    RETURN BOOLEAN
    AS
    album_saved BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO album_saved FROM user_saved_albums WHERE user_id = toggle_album_save.user_id AND album_id = toggle_album_save.album_id AND rownum <= 1;
        IF album_saved = FALSE THEN
            INSERT INTO user_saved_albums (user_id, album_id) VALUES (toggle_album_save.user_id, toggle_album_save.album_id);
            RETURN TRUE;
        ELSE
            DELETE FROM user_saved_albums WHERE user_id = toggle_album_save.user_id AND album_id = toggle_album_save.album_id;
            RETURN FALSE;
        END IF;
    END;
    /

-- Get User Genres
    SELECT
        g.genre_id
    FROM
        user_history uh
        JOIN track_genres tg ON uh.track_id = tg.track_id
        JOIN genres g ON tg.genre_id = g.genre_id
    WHERE
        uh.user_id = v_user_id;
    GROUP BY
        g.genre_id
    ORDER BY
        COUNT(g.genre_id) DESC
    FETCH FIRST 8 ROWS ONLY;


-- Get User Tracks based on pool of genres


DECLARE
    v_user_id VARCHAR2(36) := 'f9a68266-fbb1-419a-92a7-7c38afa7afb7';
    v_user_tracks SYS_REFCURSOR;
    v_history_empty BOOLEAN;
BEGIN
    SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
    IF v_history_empty = TRUE THEN
        OPEN v_user_tracks FOR
            SELECT track_id FROM tracks ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 10 ROWS ONLY;
    ELSE
        OPEN v_user_tracks FOR
            SELECT
                t.track_id
            FROM
                track_genres tg
                JOIN tracks t ON tg.track_id = t.track_id
            WHERE
                tg.genre_id IN (
                    SELECT
                        g.genre_id
                    FROM
                        user_history uh
                        JOIN track_genres tg ON uh.track_id = tg.track_id
                        JOIN genres g ON tg.genre_id = g.genre_id
                    WHERE
                        uh.user_id = v_user_id
                    GROUP BY
                        g.genre_id
                    ORDER BY
                        COUNT(g.genre_id) DESC
                    FETCH FIRST 8 ROWS ONLY
                )
            ORDER BY
                DBMS_RANDOM.VALUE
            FETCH FIRST 10 ROWS ONLY;
    END IF;
    DBMS_SQL.RETURN_RESULT(v_user_tracks);
END;
/

CREATE OR REPLACE PROCEDURE get_user_tracks(v_user_id VARCHAR2, v_user_tracks OUT SYS_REFCURSOR)
    AS
    v_history_empty BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
        IF v_history_empty = FALSE THEN
            OPEN v_user_tracks FOR
                SELECT track_id FROM tracks ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY;
        ELSE
            OPEN v_user_tracks FOR
                SELECT
                    t.track_id
                FROM
                    track_genres tg
                    JOIN tracks t ON tg.track_id = t.track_id
                WHERE
                    tg.genre_id IN (
                        SELECT
                            g.genre_id
                        FROM
                            user_history uh
                            JOIN track_genres tg ON uh.track_id = tg.track_id
                            JOIN genres g ON tg.genre_id = g.genre_id
                        WHERE
                            uh.user_id = v_user_id
                        GROUP BY
                            g.genre_id
                        ORDER BY
                            COUNT(g.genre_id) DESC
                        FETCH FIRST 8 ROWS ONLY
                    )
                ORDER BY
                    DBMS_RANDOM.VALUE
                FETCH FIRST 8 ROWS ONLY;
        END IF;
    END;
    /

-- Get User Playlists based on pool of genres
DECLARE
    v_user_id VARCHAR2(36) := 'f9a68266-fbb1-419a-92a7-7c38afa7afb7';
    v_user_playlists SYS_REFCURSOR;
    v_history_empty BOOLEAN;
BEGIN
    SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
    IF v_history_empty = TRUE THEN
        OPEN v_user_playlists FOR
            SELECT playlist_id FROM playlists ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY;
    ELSE
        OPEN v_user_playlists FOR
        SELECT
            p.playlist_id
        FROM
            playlist_tracks pt
            JOIN tracks t ON pt.track_id = t.track_id
            JOIN track_genres tg ON t.track_id = tg.track_id
            JOIN genres g ON tg.genre_id = g.genre_id
            JOIN playlists p ON pt.playlist_id = p.playlist_id
        WHERE
            g.genre_id IN (
                SELECT
                    g.genre_id
                FROM
                    user_history uh
                    JOIN track_genres tg ON uh.track_id = tg.track_id
                    JOIN genres g ON tg.genre_id = g.genre_id
                WHERE
                    uh.user_id = v_user_id
                GROUP BY
                    g.genre_id
                ORDER BY
                    COUNT(g.genre_id) DESC
                FETCH FIRST 8 ROWS ONLY
            )
        GROUP BY
            p.playlist_id
        ORDER BY
            DBMS_RANDOM.VALUE
        FETCH FIRST 5 ROWS ONLY;
    END IF;
    DBMS_SQL.RETURN_RESULT(v_user_playlists);
END;
/

CREATE OR REPLACE PROCEDURE get_user_playlists(v_user_id VARCHAR2, v_user_playlists OUT SYS_REFCURSOR)
    AS
    v_history_empty BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
        IF v_history_empty = FALSE THEN
            OPEN v_user_playlists FOR
                SELECT playlist_id FROM playlists ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY;
        ELSE
            OPEN v_user_playlists FOR
                SELECT
                    p.playlist_id
                FROM
                    playlist_tracks pt
                    JOIN tracks t ON pt.track_id = t.track_id
                    JOIN track_genres tg ON t.track_id = tg.track_id
                    JOIN genres g ON tg.genre_id = g.genre_id
                    JOIN playlists p ON pt.playlist_id = p.playlist_id
                WHERE
                    g.genre_id IN (
                        SELECT
                            g.genre_id
                        FROM
                            user_history uh
                            JOIN track_genres tg ON uh.track_id = tg.track_id
                            JOIN genres g ON tg.genre_id = g.genre_id
                        WHERE
                            uh.user_id = v_user_id
                        GROUP BY
                            g.genre_id
                        ORDER BY
                            COUNT(g.genre_id) DESC
                        FETCH FIRST 8 ROWS ONLY
                    )
                GROUP BY
                    p.playlist_id
                ORDER BY
                    DBMS_RANDOM.VALUE
                FETCH FIRST 8 ROWS ONLY;
        END IF;
    END;
    /

-- Get User Artists based on pool of genres
DECLARE
    v_user_id VARCHAR2(36) := 'f9a68266-fbb1-419a-92a7-7c38afa7afb7';
    v_user_artists SYS_REFCURSOR;
    v_history_empty BOOLEAN;
BEGIN
    SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
    IF v_history_empty = FALSE THEN
        OPEN v_user_artists FOR
            SELECT artist_id FROM artists ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY;
    ELSE
        OPEN v_user_artists FOR
        SELECT
            a.artist_id
        FROM
            artist_genres ag
            JOIN artists a ON ag.artist_id = a.artist_id
            JOIN genres g ON ag.genre_id = g.genre_id
        WHERE
            g.genre_id IN (
                SELECT
                    g.genre_id
                FROM
                    user_history uh
                    JOIN track_genres tg ON uh.track_id = tg.track_id
                    JOIN genres g ON tg.genre_id = g.genre_id
                WHERE
                    uh.user_id = v_user_id
                GROUP BY
                    g.genre_id
                ORDER BY
                    COUNT(g.genre_id) DESC
                FETCH FIRST 8 ROWS ONLY
            )
        ORDER BY
            DBMS_RANDOM.VALUE
        FETCH FIRST 5 ROWS ONLY;
    END IF;
    DBMS_SQL.RETURN_RESULT(v_user_artists);
END;
/

CREATE OR REPLACE PROCEDURE get_user_artists(v_user_id VARCHAR2, v_user_artists OUT SYS_REFCURSOR)
    AS
    v_history_empty BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
        IF v_history_empty = FALSE THEN
            OPEN v_user_artists FOR
                SELECT artist_id FROM artists ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY;
        ELSE
            OPEN v_user_artists FOR
                SELECT
                    a.artist_id
                FROM
                    artist_genres ag
                    JOIN artists a ON ag.artist_id = a.artist_id
                    JOIN genres g ON ag.genre_id = g.genre_id
                WHERE
                    g.genre_id IN (
                        SELECT
                            g.genre_id
                        FROM
                            user_history uh
                            JOIN track_genres tg ON uh.track_id = tg.track_id
                            JOIN genres g ON tg.genre_id = g.genre_id
                        WHERE
                            uh.user_id = v_user_id
                        GROUP BY
                            g.genre_id
                        ORDER BY
                            COUNT(g.genre_id) DESC
                        FETCH FIRST 8 ROWS ONLY
                    )
                ORDER BY
                    DBMS_RANDOM.VALUE
                FETCH FIRST 8 ROWS ONLY;
        END IF;
    END;
    /

-- Get User Albums based on pool of genres
DECLARE
    v_user_id VARCHAR2(36) := 'f9a68266-fbb1-419a-92a7-7c38afa7afb7';
    v_user_albums SYS_REFCURSOR;
    v_history_empty BOOLEAN;
BEGIN
    SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
    IF v_history_empty = FALSE THEN
        OPEN v_user_albums FOR
            SELECT album_id FROM albums ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY;
    ELSE
        OPEN v_user_albums FOR
        SELECT
            a.album_id
        FROM
            album_genres ag
            JOIN albums a ON ag.album_id = a.album_id
            JOIN genres g ON ag.genre_id = g.genre_id
        WHERE
            g.genre_id IN (
                SELECT
                    g.genre_id
                FROM
                    user_history uh
                    JOIN track_genres tg ON uh.track_id = tg.track_id
                    JOIN genres g ON tg.genre_id = g.genre_id
                WHERE
                    uh.user_id = v_user_id
                GROUP BY
                    g.genre_id
                ORDER BY
                    COUNT(g.genre_id) DESC
                FETCH FIRST 8 ROWS ONLY
            )
        ORDER BY
            DBMS_RANDOM.VALUE
        FETCH FIRST 5 ROWS ONLY;
    END IF;
END;
/

CREATE OR REPLACE PROCEDURE get_user_albums(v_user_id VARCHAR2, v_user_albums OUT SYS_REFCURSOR)
    AS
    v_history_empty BOOLEAN;
    BEGIN
        SELECT COUNT(*) INTO v_history_empty FROM user_history WHERE user_id = v_user_id;
        IF v_history_empty = FALSE THEN
            OPEN v_user_albums FOR
                SELECT album_id FROM albums ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY;
        ELSE
            OPEN v_user_albums FOR
                SELECT
                    a.album_id
                FROM
                    album_genres ag
                    JOIN albums a ON ag.album_id = a.album_id
                    JOIN genres g ON ag.genre_id = g.genre_id
                WHERE
                    g.genre_id IN (
                        SELECT
                            g.genre_id
                        FROM
                            user_history uh
                            JOIN track_genres tg ON uh.track_id = tg.track_id
                            JOIN genres g ON tg.genre_id = g.genre_id
                        WHERE
                            uh.user_id = v_user_id
                        GROUP BY
                            g.genre_id
                        ORDER BY
                            COUNT(g.genre_id) DESC
                        FETCH FIRST 8 ROWS ONLY
                    )
                ORDER BY
                    DBMS_RANDOM.VALUE
                FETCH FIRST 8 ROWS ONLY;
        END IF;
    END;
    /

-- Execute procedure 12ab49eb-1689-4d3a-ab8e-ab7d9a51f2c7
DECLARE
    v_user_albums SYS_REFCURSOR;
BEGIN
    get_user_albums('12ab49eb-1689-4d3a-ab8e-ab7d9a51f2c7', v_user_albums);
END;
/
