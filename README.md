<style>
:root {
    /* GitHub Dark Theme Colors */
    --gh-bg: #0d1117;
    --gh-bg-secondary: #161b22;
    --gh-text: #c9d1d9;
    --gh-text-secondary: #8b949e;
    --gh-border: #30363d;
    --gh-link: #58a6ff;
    --gh-accent: #1f6feb;
    --gh-success: #238636;
    --gh-warning: #9e6a03;
    --gh-error: #f85149;
    
    /* Material Design 3 Typography */
    --md3-font-weight-regular: 400;
    --md3-font-weight-medium: 500;
    --md3-font-weight-bold: 700;
}

.gh-theme {
    background-color: var(--gh-bg);
    color: var(--gh-text);
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
    line-height: 1.5;
    padding: 2rem;
}

.gh-card {
    background-color: var(--gh-bg-secondary);
    border: 1px solid var(--gh-border);
    border-radius: 16px;
    padding: 1.5rem;
    margin: 1rem 0;
}

.gh-table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 0;
    border-radius: 8px;
    overflow: hidden;
    margin: 1rem 0;
}

.gh-table th {
    background-color: var(--gh-bg-secondary);
    color: var(--gh-text);
    font-weight: var(--md3-font-weight-bold);
    padding: 0.75rem 1rem;
    text-align: left;
}

.gh-table td {
    padding: 0.75rem 1rem;
    border-top: 1px solid var(--gh-border);
}

.gh-table tr:hover {
    background-color: rgba(177, 186, 196, 0.12);
}

a {
    color: var(--gh-link);
    text-decoration: none;
}

a:hover {
    text-decoration: underline;
}

code {
    background-color: rgba(110, 118, 129, 0.4);
    border-radius: 6px;
    padding: 0.2em 0.4em;
    font-family: ui-monospace, SFMono-Regular, 'SF Mono', Menlo, Consolas, 'Liberation Mono', monospace;
}

h1, h2, h3, h4, h5, h6 {
    font-weight: var(--md3-font-weight-bold);
    margin-top: 1.5em;
    margin-bottom: 1em;
}

.md3-section {
    background: var(--gh-bg-secondary, #1a1f2c);
    border-radius: 28px;
    padding: 32px;
    margin: 24px 0;
    box-shadow: 0 8px 12px rgba(0, 0, 0, 0.2);
}

.md3-deps {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 16px;
    margin-top: 16px;
}

.md3-dep-card {
    background: rgba(255, 255, 255, 0.05);
    border-radius: 16px;
    padding: 16px;
    border-left: 4px solid var(--gh-link, #58a6ff);
}

.md3-license {
    background: rgba(255, 255, 255, 0.05);
    border-radius: 16px;
    padding: 24px;
    margin-top: 16px;
}

.md3-copyright {
    color: var(--gh-text-secondary, #8b949e);
    font-size: 0.9em;
    margin-top: 16px;
    padding-top: 16px;
    border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.md3-title {
    font-size: 24px;
    font-weight: 500;
    color: var(--gh-text, #c9d1d9);
    margin: 0 0 24px 0;
}

.md3-subtitle {
    font-size: 20px;
    font-weight: 500;
    color: var(--gh-text, #c9d1d9);
    margin: 24px 0 16px 0;
}

.md3-list {
    list-style: none;
    padding: 0;
    margin: 16px 0;
}

.md3-list-item {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    padding: 8px 0;
    color: var(--gh-text-secondary, #8b949e);
}

.md3-list-item::before {
    content: "•";
    color: var(--gh-link, #58a6ff);
}

.md3-alert {
    background: rgba(255, 197, 23, 0.1);
    border-left: 4px solid #ffc517;
    border-radius: 8px;
    padding: 16px;
    margin: 16px 0;
}

.md3-alert-title {
    display: flex;
    align-items: center;
    gap: 8px;
    color: #ffc517;
    font-weight: 500;
    margin-bottom: 8px;
}

.md3-code {
    background: rgba(0, 0, 0, 0.2);
    border-radius: 12px;
    padding: 16px;
    font-family: 'Roboto Mono', monospace;
    color: #c9d1d9;
    margin: 16px 0;
}

.md3-highlight {
    color: #58a6ff;
    font-weight: 500;
}

.md3-separator {
    color: #8b949e;
    margin: 0 8px;
}

.gh-code {
    background: var(--gh-bg-secondary, #161b22);
    border-radius: 16px;
    padding: 16px;
    font-family: 'Roboto Mono', monospace;
    margin: 16px 0;
}

.md3-guide {
    background: var(--gh-bg-secondary, #1a1f2c);
    border-radius: 28px;
    padding: 32px;
    margin: 24px 0;
    box-shadow: 0 8px 12px rgba(0, 0, 0, 0.2);
}

.md3-container {
    display: flex;
    gap: 24px;
    align-items: flex-start;
}

.md3-steps {
    flex: 1;
    min-width: 300px;
}

.md3-image {
    display: flex;
    justify-content: center;
    align-items: center;
    overflow: hidden
}

.md3-step {
    background: rgba(255, 255, 255, 0.05);
    border-radius: 16px;
    padding: 24px;
    margin-bottom: 16px;
}

.md3-step-title {
    font-size: 18px;
    font-weight: 500;
    color: var(--gh-text, #c9d1d9);
    margin: 0 0 16px 0;
}

.md3-step-content {
    color: var(--gh-text-secondary, #8b949e);
}
</style>

<div class="gh-theme">

# <img src="SensorsStream\favicon.ico" width="24" height="24" alt="Sensor Stream Icon"> Sensor Stream 

A real-time hardware monitoring tool that streams PC sensor data through WebSocket or UDP. 🌐

<div class="feature-container" style="display: flex; gap: 24px; background: var(--gh-bg-secondary); border-radius: 16px; padding: 24px; margin: 24px 0;">
  <div class="preview" style="flex: 1;">
    <img src="assets/examples/app.png" alt="Application Preview" style="max-width: 376px; height: 610px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
  </div>
  
  <div class="features" style="width: 100%;">
    <ul class="md3-alert" style="list-style: none;">
      <div class="md3-alert-title">🖥️ Real-time Hardware Monitoring</div>
      <li>🔥 CPU (temperature, load, clock speeds, power)</li>
      <li>🎮 GPU (NVIDIA, AMD, Intel)</li>
      <li>💾 RAM/Memory usage</li>
      <li>📦 Storage devices</li>
      <li>🌐 Network adapters</li>
      <li>🛠️ Motherboard sensors</li>
    </ul>
    <ul class="md3-alert" style="list-style: none;">
      <div class="md3-alert-title">🚀 Multiple Transport Options</div>
      <li>🔗 WebSocket server (default port 8546)</li>
      <li>📡 UDP datagrams (default port 8545)</li>
    </ul>
    <ul class="md3-alert" style="list-style: none;">
      <div class="md3-alert-title">🤝 Easy Integration</div>
      <li>📝 Simple command format: <code>component/index/value</code></li>
      <li>📜 JSON responses</li>
      <li>⏱️ Real-time updates</li>
    </ul>
    <ul class="md3-alert" style="list-style: none;">
      <div class="md3-alert-title">🔄 Configurable</div>
      <li>🔄 <del>Customizable update intervals</del> (Updating upon request now)</li>
      <li>🔍 Selective hardware monitoring</li>
      <li>🔧 Port configuration</li>
    </ul>
  </div>
</div>

<div class="md3-section">
    <h2 class="md3-title">WebSocket Command Interface</h2>
    <p>Connect to <code class="md3-code">ws://localhost:8546</code> to interact with the sensor data. 📡</p>
    <h3>Command Format</h3>
    <div class="md3-code"><b style="color: #ff7b72;">component</b>/<b style="color: #79c0ff;">index</b>/<b style="color: #7ee787;">value</b></div>
    <ul class="md3-list">
        <li>
            <span class="md3-highlight" style="color: #ff7b72;">component</span>
            <span class="md3-separator">:</span>
            Hardware type (cpu, gpu, ram, memory, storage, network, motherboard)
        </li>
        <li>
            <span class="md3-highlight" style="color: #79c0ff;">index</span>
            <span class="md3-separator">:</span>
            Component index (0-based, for multiple components of same type)
        </li>
        <li>
            <span class="md3-highlight" style="color: #7ee787;">value</span>
            <span class="md3-separator">:</span>
            Specific sensor value to retrieve
        </li>
    </ul>
</div>

<div class="gh-card">

## Value Commands
<table class="gh-table">
  <tr>
    <th colspan="3">Basic Commands</th>
    <th colspan="3">Special Commands</th>
  </tr>
  <tr>
    <td>🗝️</td>
    <td>Description</td>
    <td>Command</td>
    <td>🗝️</td>
    <td>Description</td>
    <td>Command</td>
  </tr>
  <tr>
    <td>🌡️</td>
    <td>CPU Package Temperature</td>
    <td><b style="color: #ff7b72;">cpu</b>/<b style="color: #7ee787;">0</b>/<b style="color: #79c0ff;">temperature</b>/<b style="color: #79c0ff;">package</b></td>
    <td>📋</td>
    <td>Lists all available components</td>
    <td><b style="color: #ff7b72;">system</b>/<b style="color: #79c0ff;">components</b></td>
  </tr>
  <tr>
    <td>🎮</td>
    <td>GPU Core Temperature</td>
    <td><b style="color: #ff7b72;">gpu</b>/<b style="color: #7ee787;">0</b>/<b style="color: #79c0ff;">temperature</b>/<b style="color: #79c0ff;">core</b></td>
    <td>🔍</td>
    <td>Detailed list of all components and their sensors</td>
    <td><b style="color: #ff7b72;">system</b>/<b style="color: #79c0ff;">components</b>/<b style="color: #79c0ff;">all</b></td>
  </tr>
  <tr>
    <td>💾</td>
    <td>RAM Usage</td>
    <td><b style="color: #ff7b72;">memory</b>/<b style="color: #7ee787;">0</b>/<b style="color: #79c0ff;">data</b>/<b style="color: #79c0ff;">memoryused</b></td>
    <td>📊</td>
    <td>Lists all data for specific component</td>
    <td><b style="color: #ff7b72;">{component}</b>/<b style="color: #7ee787;">{index}</b></td>
  </tr>
  <tr>
    <td>🌐</td>
    <td>Network Download Speed</td>
    <td><b style="color: #ff7b72;">network</b>/<b style="color: #7ee787;">1</b>/<b style="color: #79c0ff;">throughput</b>/<b style="color: #79c0ff;">downloadspeed</b></td>
    <td>🏷️</td>
    <td>Component name</td>
    <td><b style="color: #ff7b72;">{component}</b>/<b style="color: #7ee787;">{index}</b>/<b style="color: #79c0ff;">name</b></td>
  </tr>
  <tr>
    <td>🔧</td>
    <td>CPU Core 1 Load</td>
    <td><b style="color: #ff7b72;">cpu</b>/<b style="color: #7ee787;">0</b>/<b style="color: #79c0ff;">load</b>/<b style="color: #79c0ff;">core1</b></td>
    <td>🔢</td>
    <td>Number of sensors</td>
    <td><b style="color: #ff7b72;">{component}</b>/<b style="color: #7ee787;">{index}</b>/<b style="color: #79c0ff;">sensorcount</b></td>
  </tr>
</table>
</div>

<div class="md3-section">
    <h2 class="md3-title">Using with Resonite/ProtoFlux</h2>
    <h3 class="md3-subtitle">Prerequisites</h3>
    <ol class="md3-list">
        <li class="md3-list-item">Start SensorStream on your PC 💻</li>
        <li class="md3-list-item">Make sure WebSocket is enabled in App. (Set to desired port, default 8546) 🔗</li>
        <li class="md3-list-item">In Resonite, you'll need to request host access for WebSocket connections 🔒</li>
    </ol>
    <div class="md3-alert">
        <div class="md3-alert-title">🐾 Security Note</div>
        <p>Remember that Resonite requires user consent for WebSocket connections! When requesting access, specify that you're connecting to SensorStream to monitor PC hardware stats. ℹ️</p>
    </div>
</div>

<div class="md3-guide">
    <h2>Visual Guide: Setting up ProtoFlux for SensorStream</h2>
    <div class="md3-container">
        <div class="md3-steps">
            <div class="md3-step">
                <h3 class="md3-step-title">Step 1: Basic Setup</h3>
                <div class="md3-step-content">
                    Create these nodes:
                    <ul>
                        <li>WebsocketConnect</li>
                        <li>WebsocketTextMessageSender</li>
                        <li>WebsocketTextMessageReceiver</li>
                        <li>Parse Float</li>
                        <li>Float Display</li>
                    </ul>
                </div>
            </div>
            <div class="md3-step">
                <h3 class="md3-step-title">Step 2: Connection Setup</h3>
                <div class="md3-step-content">
                    Configure WebsocketConnect:
                    <ul>
                        <li>Set URI to "ws://localhost:8546"</li>
                        <li>Connect 'Connected' output to MessageSender</li>
                    </ul>
                </div>
            </div>
            <div class="md3-step">
                <h3 class="md3-step-title">Step 3: Message Flow</h3>
                <div class="md3-step-content">
                    Connect in sequence:
                    <ol>
                        <li>Connect → MessageSender</li>
                        <li>MessageSender → MessageReceiver</li>
                        <li>MessageReceiver → Parse Float</li>
                        <li>Parse Float → Float Display</li>
                    </ol>
                </div>
            </div>
        </div>
        <div class="md3-image">
            <img src="assets/examples/basic setup.jpg" alt="ProtoFlux Setup Diagram">
        </div>
    </div>
</div>

<div class="md3-section">
    <h2>Dependencies</h2>
    <div class="md3-deps">
        <div class="md3-dep-card">
            <img src="https://pics.computerbase.de/9/8/1/5/4-ef5489c86322778d/logo-256.png" width="24" height="24" alt="LibreHardwareMonitorLib Icon">
            <h3>LibreHardwareMonitorLib</h3>
            <p>Hardware monitoring functionality</p>
        </div>
        <div class="md3-dep-card">
            <img src="https://cdn.worldvectorlogo.com/logos/websocket.svg" width="24" height="24" alt="Fleck Icon" style="filter: invert(100%)">
            <h3>Fleck</h3>
            <p>WebSocket server implementation</p>
        </div>
        <div class="md3-dep-card">
            <img src="https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.3/icon" width="24" height="24" alt="Newtonsoft.Json Icon" style="filter: invert(100%)">
            <h3>Newtonsoft.Json</h3>
            <p>JSON serialization support</p>
        </div>
        <div class="md3-dep-card">
            <img src="https://upload.wikimedia.org/wikipedia/commons/2/25/NuGet_project_logo.svg" width="24" height="24" alt="TaskScheduler Icon" style="filter: invert(50%)"> 
            <h3>HidSharp</h3>
            <p>Hardware interface management</p>
        </div>
        <div class="md3-dep-card">
            <img src="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQHDsn-XqBRaQVnHE6QkbsHNw_OtaIMe457Rw&s" width="24" height="24" alt="QRCoder Icon" ><h3>QRCoder</h3>
            <p>Hardware interface management</p>
        </div>
        <div class="md3-dep-card">
            <img src="https://upload.wikimedia.org/wikipedia/commons/2/25/NuGet_project_logo.svg" width="24" height="24" alt="TaskScheduler Icon" style="filter: invert(40%)"> 
            <h3>TaskScheduler</h3>
            <p>Hardware interface management</p>
        </div>
    </div>
</div>

<div class="md3-section">
    <h2>License</h2>
    <div class="md3-license">
        <h3>MIT License</h3>
        <div class="md3-copyright">
            <p>Copyright (c) 2021 Jecsham Castillo | Copyright (c) 2024 Dexy</p>
            <p>This software is built upon the original work by Jecsham Castillo</p>
        </div>
    </div>
</div>

</div>
